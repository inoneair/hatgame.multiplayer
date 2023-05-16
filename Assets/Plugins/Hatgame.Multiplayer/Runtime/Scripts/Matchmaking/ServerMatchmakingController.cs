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
            _matchmakingData = new ServerMatchmakingData(settings.maxPlayersPerLobby, settings.maxLobbyCount);
            _networkController = networkController;
            _networkController.RegisterOnServerConnect(OnServerConnectHandler);
            _networkController.RegisterOnServerDisconnect(OnServerDisconnectHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestCreateLobbyMessage>(OnRequestCreateLobbyMessageHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestJoinLobbyMessage>(OnRequestJoinLobbyMessageHandler);
            _networkController.RegisterServerOnReceiveMessage<RequestLeaveLobbyMessage>(OnRequestLeaveLobbyMessageHandler);
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
                OnPlayerLeaveLobbyHandler(playerId);
                _matchmakingData.RemovePlayer(playerId);
                _connectionToPlayerId.Remove(connection.connectionId);
                _playerIdToConnection.Remove(playerId);
            }
        }

        private void OnRequestCreateLobbyMessageHandler(NetworkConnectionToClient connection, RequestCreateLobbyMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
                isSuccess = _matchmakingData.CreateLobby(message.lobbyName, playerId);

            _networkController.SendMessageToClient(connection, new AnswerCreateLobbyMessage { isSuccess = isSuccess });
        }

        private void OnRequestJoinLobbyMessageHandler(NetworkConnectionToClient connection, RequestJoinLobbyMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
            {
                isSuccess = _matchmakingData.AddPlayerToLobby(playerId, message.lobbyName);
                if (isSuccess)
                {
                    var joinedPlayer = _matchmakingData.GetPlayer(playerId).Value;
                    var lobbyPlayers = _matchmakingData.GetLobbyPlayers(message.lobbyName);
                    var onOtherPlayerJoinLobbyMessage = new OnOtherPlayerJoinLobbyMessage { playerId = joinedPlayer.id, playerName = joinedPlayer.name };
                    foreach (var player in lobbyPlayers)
                    {
                        if (player != joinedPlayer.id)
                            _networkController.SendMessageToClient(_playerIdToConnection[player], onOtherPlayerJoinLobbyMessage);
                    }
                }
            }

            _networkController.SendMessageToClient(connection, new AnswerJoinLobbyMessage { isSuccess = isSuccess });
        }

        private void OnRequestLeaveLobbyMessageHandler(NetworkConnectionToClient connection, RequestLeaveLobbyMessage message)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.connectionId, out var playerId))
                isSuccess = OnPlayerLeaveLobbyHandler(playerId);

            _networkController.SendMessageToClient(connection, new AnswerLeaveLobbyMessage { isSuccess = isSuccess });
        }

        private bool OnPlayerLeaveLobbyHandler(uint playerId)
        {
            var player = _matchmakingData.GetPlayer(playerId);
            if (player != null && !string.IsNullOrEmpty(player.Value.lobbyName))
            {
                var isAdmin = false;
                if (_matchmakingData.TryGetLobbyAdmin(player.Value.lobbyName, out uint adminId))
                    isAdmin = playerId == adminId;

                var isLeaveSuccess = _matchmakingData.RemovePlayerFromLobby(playerId);
                var lobbyPlayers = _matchmakingData.GetLobbyPlayers(player.Value.lobbyName);
                if (lobbyPlayers.Length > 0)
                {
                    var message = new OnOtherPlayerLeaveLobbyMessage { playerId = playerId };
                    foreach (var lobbyPlayer in lobbyPlayers)
                        _networkController.SendMessageToClient(_playerIdToConnection[lobbyPlayer], message);
                }

                if (isLeaveSuccess && isAdmin && _matchmakingData.TryGetLobbyAdmin(player.Value.name, out uint newAdminId))
                {
                    if (_playerIdToConnection.TryGetValue(newAdminId, out int newAdminConnection))
                        _networkController.SendMessageToClient(newAdminConnection, new OnReceivedLobbyAdminRights());
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
                    if (!string.IsNullOrWhiteSpace(player.Value.lobbyName))
                    {
                        var lobbyPlayers = _matchmakingData.GetLobbyPlayers(player.Value.lobbyName);
                        if (lobbyPlayers.Length > 0)
                        {
                            var onOtherPlayerChangeNameMessage = new OnOtherPlayerChangeNameMessage { playerId = playerId, newPlayerName = message.newPlayerName };
                            foreach (var lobbyPlayer in lobbyPlayers)
                            {
                                if (lobbyPlayer != playerId)
                                    _networkController.SendMessageToClient(_playerIdToConnection[lobbyPlayer], onOtherPlayerChangeNameMessage);
                            }
                        }
                    }
                }
            }

            _networkController.SendMessageToClient(connection, new AnswerChangePlayerNameMessage { isSuccess = isSuccess });
        }
    }
}
