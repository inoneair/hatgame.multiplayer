using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hatgame.Multiplayer
{
    public class ClientMacthmakingController
    {
        private ClientMatchmakingData _matchmakingData;
        private NetworkController _networkController;

        private event Action<AnswerStartGameMessage> _onAnswerStartGameReceived;
        private event Action<AnswerCreateLobbyMessage> _onAnswerCreateLobbyReceived;
        private event Action<AnswerJoinLobbyMessage> _onAnswerJoinLobbyReceived;
        private event Action<AnswerLeaveLobbyMessage> _onAnswerLeaveLobbyReceived;
        private event Action<AnswerChangePlayerNameMessage> _onAnswerChangePlayerNameReceived;

        public ClientMacthmakingController()
        {
            _matchmakingData = new ClientMatchmakingData();
            _networkController = NetworkController.instance;

            _networkController.RegisterOnClientDisconnect(OnClientDisconnectHandler);
            _networkController.RegisterClientOnReceiveMessage<OnPlayerConnectedMessage>(OnPlayerConnectedMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<OnReceivedLobbyAdminRights>(OnReceivedLobbyAdminRights);

            _networkController.RegisterClientOnReceiveMessage<AnswerStartGameMessage>(OnAnswerStartGameMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerCreateLobbyMessage>(OnAnswerCreateLobbyMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerJoinLobbyMessage>(OnAnswerJoinLobbyMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerLeaveLobbyMessage>(OnAnswerLeaveLobbyMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerChangePlayerNameMessage>(OnAnswerChangePlayerNameMessageHandler);

            _networkController.RegisterClientOnReceiveMessage<OnOtherPlayerJoinLobbyMessage>(OnOtherPlayerJoinLobbyMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<OnOtherPlayerLeaveLobbyMessage>(OnOtherPlayerLeaveLobbyMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<OnOtherPlayerChangeNameMessage>(OnOtherPlayerChangeNameMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<OnAdminStartGameMessage>(OnAdminStartGameMessageHandler);
        }

        public async Task<bool> StartGame()
        {
            _networkController.SendMessageToServer(new RequestStartGameMessage());

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerStartGameMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerMessage) =>
            {
                if (answerMessage.isSuccess)
                {
                    isSuccess = true;
                }

                _onAnswerStartGameReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerStartGameReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        public async Task<bool> CreateLobby(string lobbyName)
        {
            _networkController.SendMessageToServer(new RequestCreateLobbyMessage { lobbyName = lobbyName });

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerCreateLobbyMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerCreateLobbyMessage) =>
            {
                if (answerCreateLobbyMessage.isSuccess)
                {
                    isSuccess = true;
                    _matchmakingData.currentLobby = lobbyName;
                    _matchmakingData.isAdmin = true;
                }

                _onAnswerCreateLobbyReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerCreateLobbyReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        public async Task<bool> JoinLobby(string lobbyName)
        {
            _networkController.SendMessageToServer(new RequestJoinLobbyMessage { lobbyName = lobbyName });

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerJoinLobbyMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerJoinLobbyMessage) =>
            {
                if (answerJoinLobbyMessage.isSuccess)
                {
                    isSuccess = true;
                    foreach (var player in answerJoinLobbyMessage.players)
                        _matchmakingData.AddOtherPlayerToLobby(player);

                    _matchmakingData.currentLobby = lobbyName;
                    _matchmakingData.isAdmin = true;
                }

                _onAnswerJoinLobbyReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerJoinLobbyReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        public async Task<bool> LeaveLobby()
        {
            _networkController.SendMessageToServer(new RequestLeaveLobbyMessage());

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerLeaveLobbyMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerLeaveLobbyMessage) =>
            {
                if (answerLeaveLobbyMessage.isSuccess)
                {
                    isSuccess = true;
                    _matchmakingData.currentLobby = string.Empty;
                    _matchmakingData.ClearOtherLobbyPlayers();
                }

                _onAnswerLeaveLobbyReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerLeaveLobbyReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        public async Task<bool> ChangePlayerName(string newName)
        {
            _networkController.SendMessageToServer(new RequestChangePlayerNameMessage { newPlayerName = newName });

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerChangePlayerNameMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerLeaveLobbyMessage) =>
            {
                if (answerLeaveLobbyMessage.isSuccess)
                {
                    isSuccess = true;
                    var player = _matchmakingData.player;
                    player.name = newName;
                    _matchmakingData.player = player;
                }

                _onAnswerChangePlayerNameReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerChangePlayerNameReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        private void OnClientDisconnectHandler() =>
            _matchmakingData.Reset();

        private void OnPlayerConnectedMessageHandler(OnPlayerConnectedMessage message) =>
            _matchmakingData.player = message.player;

        private void OnReceivedLobbyAdminRights(OnReceivedLobbyAdminRights message)
        {
            _matchmakingData.isAdmin = true;
        }

        private void OnAnswerStartGameMessageHandler(AnswerStartGameMessage message) =>
            _onAnswerStartGameReceived?.Invoke(message);

        private void OnAnswerCreateLobbyMessageHandler(AnswerCreateLobbyMessage message) =>
            _onAnswerCreateLobbyReceived?.Invoke(message);

        private void OnAnswerJoinLobbyMessageHandler(AnswerJoinLobbyMessage message) =>
            _onAnswerJoinLobbyReceived?.Invoke(message);

        private void OnAnswerLeaveLobbyMessageHandler(AnswerLeaveLobbyMessage message) =>
            _onAnswerLeaveLobbyReceived?.Invoke(message);

        private void OnAnswerChangePlayerNameMessageHandler(AnswerChangePlayerNameMessage message) =>
            _onAnswerChangePlayerNameReceived?.Invoke(message);

        private void OnOtherPlayerJoinLobbyMessageHandler(OnOtherPlayerJoinLobbyMessage message)
        {
            _matchmakingData.AddOtherPlayerToLobby(new MatchmakingPlayer { id = message.playerId, name = message.playerName, lobbyName = _matchmakingData.currentLobby });
        }

        private void OnOtherPlayerLeaveLobbyMessageHandler(OnOtherPlayerLeaveLobbyMessage message)
        {
            _matchmakingData.RemoveOtherPlayerFromLobby(message.playerId);
        }

        private void OnOtherPlayerChangeNameMessageHandler(OnOtherPlayerChangeNameMessage message)
        {
            _matchmakingData.OtherPlayerChangeName(message.playerId, message.newPlayerName);
        }

        private void OnAdminStartGameMessageHandler(OnAdminStartGameMessage message)
        {

        }
    }
}
