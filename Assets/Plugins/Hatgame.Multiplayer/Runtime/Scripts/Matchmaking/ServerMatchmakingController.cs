using System;
using System.Collections;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class ServerMatchmakingController
    {
        private ServerNetworkController _networkController;
        private ServerMatchmakingData _matchmakingData;

        private Dictionary<int, uint> _connectionToPlayerId = new Dictionary<int, uint>();
        private Dictionary<uint, INetworkConnection> _playerIdToConnection = new Dictionary<uint, INetworkConnection>();

        private ServerMatchmakingController(ServerNetworkController networkController, ListenerStorage messageListener)
        {
            _networkController = networkController;
            _matchmakingData = new ServerMatchmakingData();
            _networkController.SubscribeOnNewConnectionEstablished(OnNewConnectionEstablishedHandler);
            _networkController.SubscribeOnClientDisconnected(OnClientDisconnectedHandler);
            _networkController.SubscribeOnMessageReceived<RequestCreateLobbyMessage>(OnRequestCreateLobbyMessageHandler);
            _networkController.SubscribeOnMessageReceived<RequestJoinLobbyMessage>(OnRequestJoinLobbyMessageHandler);
            _networkController.SubscribeOnMessageReceived<RequestLeaveLobbyMessage>(OnRequestLeaveLobbyMessageHandler);
            _networkController.SubscribeOnMessageReceived<RequestChangePlayerNameMessage>(OnRequestChangePlayerNameMessageHandler);
        }

        public void SetSettings(MatchmakingSettings settings)
        {
            _matchmakingData.maxPlayersPerLobby = settings.maxPlayersPerLobby;
            _matchmakingData.maxLobbyCount = settings.maxLobbyCount;
        }

        private void OnNewConnectionEstablishedHandler(INetworkConnection connection)
        {
            var player = _matchmakingData.AddPlayer("");
            _connectionToPlayerId.Add(connection.id, player.id);
            _playerIdToConnection.Add(player.id, connection);
            var answer = new OnPlayerConnectedMessage { player = player };
            _networkController.SendMessage(answer, connection);
        }

        private void OnClientDisconnectedHandler(INetworkConnection connection)
        {
            if (_connectionToPlayerId.TryGetValue(connection.id, out var playerId))
            {
                OnPlayerLeaveLobbyHandler(playerId);
                _matchmakingData.RemovePlayer(playerId);
                _connectionToPlayerId.Remove(connection.id);
                _playerIdToConnection.Remove(playerId);
            }
        }

        private void OnRequestCreateLobbyMessageHandler(RequestCreateLobbyMessage message, INetworkConnection connection)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.id, out var playerId))
                isSuccess = _matchmakingData.CreateLobby(message.lobbyName, playerId);

            _networkController.SendMessage(new AnswerCreateLobbyMessage { isSuccess = isSuccess }, connection);
        }

        private void OnRequestJoinLobbyMessageHandler(RequestJoinLobbyMessage message, INetworkConnection connection)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.id, out var playerId))
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
                            _networkController.SendMessage(onOtherPlayerJoinLobbyMessage, _playerIdToConnection[player]);
                    }
                }
            }

            _networkController.SendMessage(new AnswerJoinLobbyMessage { isSuccess = isSuccess }, connection);
        }

        private void OnRequestLeaveLobbyMessageHandler(RequestLeaveLobbyMessage message, INetworkConnection connection)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.id, out var playerId))
                isSuccess = OnPlayerLeaveLobbyHandler(playerId);

            _networkController.SendMessage(new AnswerLeaveLobbyMessage { isSuccess = isSuccess }, connection);
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
                        _networkController.SendMessage(message, _playerIdToConnection[lobbyPlayer]);
                }

                if (isLeaveSuccess && isAdmin && _matchmakingData.TryGetLobbyAdmin(player.Value.name, out uint newAdminId))
                {
                    if (_playerIdToConnection.TryGetValue(newAdminId, out var newAdminConnection))
                        _networkController.SendMessage(new OnReceivedLobbyAdminRights(), newAdminConnection);
                }

                return isLeaveSuccess;
            }

            return false;
        }

        private void OnRequestChangePlayerNameMessageHandler(RequestChangePlayerNameMessage message, INetworkConnection connection)
        {
            bool isSuccess = false;
            if (_connectionToPlayerId.TryGetValue(connection.id, out var playerId))
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
                                    _networkController.SendMessage(onOtherPlayerChangeNameMessage, _playerIdToConnection[lobbyPlayer]);
                            }
                        }
                    }
                }
            }

            _networkController.SendMessage(new AnswerChangePlayerNameMessage { isSuccess = isSuccess }, connection);
        }
    }
}
