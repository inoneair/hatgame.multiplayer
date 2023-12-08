using System;
using System.Threading.Tasks;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    public class ClientMacthmakingController
    {
        private ClientMatchmakingData _matchmakingData;
        //private MirrorNetworkController _networkController;

        private Action _onClientConnected;
        private Action _onClientDisconnected;

        private Action<AnswerStartGameMessage> _onAnswerStartGameReceived;
        private Action<AnswerCreateLobbyMessage> _onAnswerCreateLobbyReceived;
        private Action<AnswerJoinLobbyMessage> _onAnswerJoinLobbyReceived;
        private Action<AnswerLeaveLobbyMessage> _onAnswerLeaveLobbyReceived;
        private Action<AnswerChangePlayerNameMessage> _onAnswerChangePlayerNameReceived;

       // public bool isConnected => _networkController.connectionsCount > 0;
        
        private static ClientMacthmakingController _instance;

        public static ClientMacthmakingController instance => _instance ??= new ClientMacthmakingController();
        

        public ClientMacthmakingController()
        {
            _matchmakingData = new ClientMatchmakingData();
            /*_networkController = MirrorNetworkController.instance;

            _networkController.SubscribeOnClientConnect(OnClientConnectHandler);
            _networkController.SubscribeOnClientDisconnect(OnClientDisconnectHandler);
            _networkController.SubscribeClientOnReceiveMessage<OnPlayerConnectedMessage>(OnPlayerConnectedMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<OnReceivedLobbyAdminRights>(OnReceivedLobbyAdminRights);

            _networkController.SubscribeClientOnReceiveMessage<AnswerStartGameMessage>(OnAnswerStartGameMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<AnswerCreateLobbyMessage>(OnAnswerCreateLobbyMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<AnswerJoinLobbyMessage>(OnAnswerJoinLobbyMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<AnswerLeaveLobbyMessage>(OnAnswerLeaveLobbyMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<AnswerChangePlayerNameMessage>(OnAnswerChangePlayerNameMessageHandler);

            _networkController.SubscribeClientOnReceiveMessage<OnOtherPlayerJoinLobbyMessage>(OnOtherPlayerJoinLobbyMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<OnOtherPlayerLeaveLobbyMessage>(OnOtherPlayerLeaveLobbyMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<OnOtherPlayerChangeNameMessage>(OnOtherPlayerChangeNameMessageHandler);
            _networkController.SubscribeClientOnReceiveMessage<OnAdminStartGameMessage>(OnAdminStartGameMessageHandler);*/
        }

        public async Task<bool> StartGame()
        {
            //_networkController.SendMessageToServer(new RequestStartGameMessage());

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

            /*while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();*/

            return isSuccess;
        }

        public async Task<bool> CreateLobby(string lobbyName)
        {
            //_networkController.SendMessageToServer(new RequestCreateLobbyMessage { lobbyName = lobbyName });

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

            /*while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();*/

            return isSuccess;
        }

        public async Task<bool> JoinLobby(string lobbyName)
        {
            //_networkController.SendMessageToServer(new RequestJoinLobbyMessage { lobbyName = lobbyName });

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

            /*while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();*/

            return isSuccess;
        }

        public async Task<bool> LeaveLobby()
        {
            //_networkController.SendMessageToServer(new RequestLeaveLobbyMessage());

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

            /*while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();*/

            return isSuccess;
        }

        public async Task<bool> ChangePlayerName(string newName)
        {
            //_networkController.SendMessageToServer(new RequestChangePlayerNameMessage { newPlayerName = newName });

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

            /*while (_networkController.isNetworkActive && answerReceived)
                await Task.Yield();*/

            return isSuccess;
        }

        public IDisposable SubscribeOnClientConnected(Action handler)
        {
            _onClientConnected += handler;

            return new Unsubscriber(()=> _onClientConnected -= handler);
        }

        public IDisposable SubscribeOnClientDisconnected(Action handler)
        {
            _onClientDisconnected += handler;

            return new Unsubscriber(() => _onClientDisconnected -= handler);
        }

        private void OnClientConnectHandler()
        {
            _onClientConnected?.Invoke();
        }

        private void OnClientDisconnectHandler()
        {
            _matchmakingData.Reset();

            _onClientDisconnected?.Invoke();
        }

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

        private void OnOtherPlayerJoinLobbyMessageHandler(OnOtherPlayerJoinLobbyMessage message) =>        
            _matchmakingData.AddOtherPlayerToLobby(new MatchmakingPlayer { id = message.playerId, name = message.playerName, lobbyName = _matchmakingData.currentLobby });
        
        private void OnOtherPlayerLeaveLobbyMessageHandler(OnOtherPlayerLeaveLobbyMessage message) =>        
            _matchmakingData.RemoveOtherPlayerFromLobby(message.playerId);
        
        private void OnOtherPlayerChangeNameMessageHandler(OnOtherPlayerChangeNameMessage message) =>        
            _matchmakingData.OtherPlayerChangeName(message.playerId, message.newPlayerName);
        
        private void OnAdminStartGameMessageHandler(OnAdminStartGameMessage message)
        {

        }
    }
}
