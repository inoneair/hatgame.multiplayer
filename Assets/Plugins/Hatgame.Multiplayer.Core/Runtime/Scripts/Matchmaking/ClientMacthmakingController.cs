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
        private event Action<AnswerCreateRoomMessage> _onAnswerCreateRoomReceived;
        private event Action<AnswerJoinRoomMessage> _onAnswerJoinRoomReceived;
        private event Action<AnswerLeaveRoomMessage> _onAnswerLeaveRoomReceived;
        private event Action<AnswerChangePlayerNameMessage> _onAnswerChangePlayerNameReceived;

        public ClientMacthmakingController(NetworkController networkController)
        {
            _matchmakingData = new ClientMatchmakingData();
            _networkController = networkController;

            _networkController.RegisterOnClientDisconnect(OnClientDisconnectHandler);
            _networkController.RegisterClientOnReceiveMessage<OnPlayerConnectedMessage>(OnPlayerConnectedMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<OnReceivedRoomAdminRights>(OnReceivedRoomAdminRights);

            _networkController.RegisterClientOnReceiveMessage<AnswerStartGameMessage>(OnAnswerStartGameMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerCreateRoomMessage>(OnAnswerCreateRoomMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerJoinRoomMessage>(OnAnswerJoinRoomMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerLeaveRoomMessage>(OnAnswerLeaveRoomMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<AnswerChangePlayerNameMessage>(OnAnswerChangePlayerNameMessageHandler);

            _networkController.RegisterClientOnReceiveMessage<OnOtherPlayerJoinRoomMessage>(OnOtherPlayerJoinRoomMessageHandler);
            _networkController.RegisterClientOnReceiveMessage<OnOtherPlayerLeaveRoomMessage>(OnOtherPlayerLeaveRoomMessageHandler);
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

        public async Task<bool> CreateRoom(string roomName)
        {
            _networkController.SendMessageToServer(new RequestCreateRoomMessage { roomName = roomName });

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerCreateRoomMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerCreateRoomMessage) =>
            {
                if (answerCreateRoomMessage.isSuccess)
                {
                    isSuccess = true;
                    _matchmakingData.currentRoom = roomName;
                    _matchmakingData.isAdmin = true;
                }

                _onAnswerCreateRoomReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerCreateRoomReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        public async Task<bool> JoinRoom(string roomName)
        {
            _networkController.SendMessageToServer(new RequestJoinRoomMessage { roomName = roomName });

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerJoinRoomMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerJoinRoomMessage) =>
            {
                if (answerJoinRoomMessage.isSuccess)
                {
                    isSuccess = true;
                    foreach (var roomPlayer in answerJoinRoomMessage.roomPlayers)
                        _matchmakingData.AddOtherPlayerToRoom(roomPlayer);

                    _matchmakingData.currentRoom = roomName;
                    _matchmakingData.isAdmin = true;
                }

                _onAnswerJoinRoomReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerJoinRoomReceived += onAnswerReceivedHandler;

            while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();

            return isSuccess;
        }

        public async Task<bool> LeaveRoom()
        {
            _networkController.SendMessageToServer(new RequestLeaveRoomMessage());

            bool isSuccess = false;
            bool answerReceived = false;
            Action<AnswerLeaveRoomMessage> onAnswerReceivedHandler = null;
            onAnswerReceivedHandler = (answerLeaveRoomMessage) =>
            {
                if (answerLeaveRoomMessage.isSuccess)
                {
                    isSuccess = true;
                    _matchmakingData.currentRoom = string.Empty;
                    _matchmakingData.ClearOtherRoomPlayers();
                }

                _onAnswerLeaveRoomReceived -= onAnswerReceivedHandler;
                answerReceived = true;
            };
            _onAnswerLeaveRoomReceived += onAnswerReceivedHandler;

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
            onAnswerReceivedHandler = (answerLeaveRoomMessage) =>
            {
                if (answerLeaveRoomMessage.isSuccess)
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

        private void OnReceivedRoomAdminRights(OnReceivedRoomAdminRights message)
        {
            _matchmakingData.isAdmin = true;
        }

        private void OnAnswerStartGameMessageHandler(AnswerStartGameMessage message) =>
            _onAnswerStartGameReceived?.Invoke(message);

        private void OnAnswerCreateRoomMessageHandler(AnswerCreateRoomMessage message) =>
            _onAnswerCreateRoomReceived?.Invoke(message);

        private void OnAnswerJoinRoomMessageHandler(AnswerJoinRoomMessage message) =>
            _onAnswerJoinRoomReceived?.Invoke(message);

        private void OnAnswerLeaveRoomMessageHandler(AnswerLeaveRoomMessage message) =>
            _onAnswerLeaveRoomReceived?.Invoke(message);

        private void OnAnswerChangePlayerNameMessageHandler(AnswerChangePlayerNameMessage message) =>
            _onAnswerChangePlayerNameReceived?.Invoke(message);

        private void OnOtherPlayerJoinRoomMessageHandler(OnOtherPlayerJoinRoomMessage message)
        {
            _matchmakingData.AddOtherPlayerToRoom(new MatchmakingPlayer { id = message.playerId, name = message.playerName, roomName = _matchmakingData.currentRoom });
        }

        private void OnOtherPlayerLeaveRoomMessageHandler(OnOtherPlayerLeaveRoomMessage message)
        {
            _matchmakingData.RemoveOtherPlayerFromRoom(message.playerId);
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
