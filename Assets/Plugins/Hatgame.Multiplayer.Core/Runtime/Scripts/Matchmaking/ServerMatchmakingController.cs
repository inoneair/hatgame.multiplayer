using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;

namespace Hatgame.Multiplayer
{
    public class ServerMatchmakingController
    {
        private ServerMatchmakingData _matchmakingData;
        private NetworkController _networkController;

        private Dictionary<int, uint> _connectionToPlayerId = new Dictionary<int, uint>();
        private Dictionary<uint, int> _playerIdToConnection = new Dictionary<uint, int>();

        public ServerMatchmakingController(NetworkController networkController, MatchmakingSettings settings)
        {
            _matchmakingData = new ServerMatchmakingData(settings.maxPlayersPerRoom, settings.maxRoomCount);
            _networkController = networkController;
            _networkController.RegisterOnServerConnect(OnServerConnectHandler);
            _networkController.RegisterOnServerDisconnect(OnServerDisconnectHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestCreateRoomMessage>(OnRequestCreateRoomMessageHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestJoinRoomMessage>(OnRequestJoinRoomMessageHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestLeaveRoomMessage>(OnRequestLeaveRoomMessageHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestChangePlayerNameMessage>(OnRequestChangePlayerNameMessageHandler);
        }

        private void OnServerConnectHandler(NetworkConnectionToClient connection)
        {
            var player = _matchmakingData.AddPlayer("");
            _connectionToPlayerId.Add(connection.connectionId, player.id);
            _playerIdToConnection.Add(player.id, connection.connectionId);
            var answer = new OnPlayerConnectedMessage { player = player };
            NetworkController.instance.SendMessageToClient(connection, answer);
        }

        private void OnServerDisconnectHandler(NetworkConnectionToClient connection)
        {
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
            {
                OnPlayerLeaveRoomHandler(playerId);
                _matchmakingData.RemovePlayer(playerId);
                _connectionToPlayerId.Remove(connection.connectionId);
                _playerIdToConnection.Remove(playerId);
            }
        }

        private void OnRequestCreateRoomMessageHandler(NetworkConnectionToClient connection, RequestCreateRoomMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
                isSuccess = _matchmakingData.CreateRoom(message.roomName, playerId);

            _networkController.SendMessageToClient(connection, new AnswerCreateRoomMessage { isSuccess = isSuccess });
        }

        private void OnRequestJoinRoomMessageHandler(NetworkConnectionToClient connection, RequestJoinRoomMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
            {
                isSuccess = _matchmakingData.AddPlayerToRoom(playerId, message.roomName);
                if (isSuccess)
                {
                    var joinedPlayer = _matchmakingData.GetPlayer(playerId).Value;
                    var roomPlayers = _matchmakingData.GetRoomPlayers(message.roomName);
                    var onOtherPlayerJoinRoomMessage = new OnOtherPlayerJoinRoomMessage { playerId = joinedPlayer.id, playerName = joinedPlayer.name };
                    foreach (var roomPlayer in roomPlayers)
                    {
                        if (roomPlayer != joinedPlayer.id)
                            _networkController.SendMessageToClient(_playerIdToConnection[roomPlayer], onOtherPlayerJoinRoomMessage);
                    }
                }
            }

            _networkController.SendMessageToClient(connection, new AnswerJoinRoomMessage { isSuccess = isSuccess });
        }

        private void OnRequestLeaveRoomMessageHandler(NetworkConnectionToClient connection, RequestLeaveRoomMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
                isSuccess = OnPlayerLeaveRoomHandler(playerId);

            _networkController.SendMessageToClient(connection, new AnswerLeaveRoomMessage { isSuccess = isSuccess });
        }

        private bool OnPlayerLeaveRoomHandler(uint playerId)
        {
            var player = _matchmakingData.GetPlayer(playerId);
            if (player != null && !string.IsNullOrEmpty(player.Value.roomName))
            {
                var isAdmin = false;
                if (_matchmakingData.TryGetRoomAdmin(player.Value.roomName, out uint adminId))
                    isAdmin = playerId == adminId;

                var isLeaveSuccess = _matchmakingData.RemovePlayerFromRoom(playerId);
                var roomPlayers = _matchmakingData.GetRoomPlayers(player.Value.roomName);
                if (roomPlayers.Length > 0)
                {
                    var message = new OnOtherPlayerLeaveRoomMessage { playerId = playerId };
                    foreach (var roomPlayer in roomPlayers)
                        _networkController.SendMessageToClient(_playerIdToConnection[roomPlayer], message);
                }

                if (isLeaveSuccess && isAdmin && _matchmakingData.TryGetRoomAdmin(player.Value.name, out uint newAdminId))
                {
                    if (_playerIdToConnection.TryGetValue(newAdminId, out int newAdminConnection))
                        _networkController.SendMessageToClient(newAdminConnection, new OnReceivedRoomAdminRights());
                }

                return isLeaveSuccess;
            }

            return false;
        }

        private void OnRequestChangePlayerNameMessageHandler(NetworkConnectionToClient connection, RequestChangePlayerNameMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
            {
                isSuccess = _matchmakingData.ChangePlayerName(playerId, message.newPlayerName);
                if (isSuccess)
                {
                    var player = _matchmakingData.GetPlayer(playerId);
                    if (!string.IsNullOrWhiteSpace(player.Value.roomName))
                    {
                        var roomPlayers = _matchmakingData.GetRoomPlayers(player.Value.roomName);
                        if (roomPlayers.Length > 0)
                        {
                            var onOtherPlayerChangeNameMessage = new OnOtherPlayerChangeNameMessage { playerId = playerId, newPlayerName = message.newPlayerName };
                            foreach (var roomPlayer in roomPlayers)
                            {
                                if (roomPlayer != playerId)
                                    _networkController.SendMessageToClient(_playerIdToConnection[roomPlayer], onOtherPlayerChangeNameMessage);
                            }
                        }
                    }
                }
            }

            _networkController.SendMessageToClient(connection, new AnswerChangePlayerNameMessage { isSuccess = isSuccess });
        }
    }
}
