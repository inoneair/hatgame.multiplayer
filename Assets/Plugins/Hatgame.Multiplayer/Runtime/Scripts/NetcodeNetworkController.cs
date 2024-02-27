using System;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    /*public class NetcodeNetworkController
    {
        public enum NetworkMode { Offline, Server, Client}

        IServerBehaviour _serverBehaviour;
        IClientBehaviour _clientBehaviour;

        static private NetworkConnection _clientReadyConnection;

        private string _networkAddress = "localhost";

        private Action _onStartServer;
        private Action _onStartClient;
        private Action _onStopServer;
        private Action _onStopClient;

        private Action<NetworkConnection> _onServerConnect;
        private Action _onClientConnect;

        private Action<NetworkConnection> _onServerDisconnect;
        private Action _onClientDisconnect;

        private Action<NetworkConnection, TransportError, string> _onServerError;
        private Action<TransportError, string> _onClientError;

        private ListenerStorage _serverMessageListeners = new ListenerStorage();
        private ListenerStorage _clientMessageListeners = new ListenerStorage();

        public int serverTickRate
        {
            get => _serverBehaviour.tickRate;
            set => _serverBehaviour.tickRate = value;
        }

        public int maxConnections
        {
            get => _serverBehaviour.maxConnections;
            set => _serverBehaviour.maxConnections = Mathf.Max(value, 0);
        }

        public int connectionsCount => _serverBehaviour.connectionsCount;

        public bool isNetworkActive => _serverBehaviour.isActive || NetworkClient.active;

        public NetworkMode mode { get; private set; }

        public NetcodeNetworkController()
        {
            serverTickRate = 30;
        }

        public void StartServer()
        {
            if (_serverBehaviour.isActive)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

            mode = NetworkMode.Server;

            SetupServer();
            _onStartServer?.Invoke();
        }

        public void StartClient()
        {
            if (NetworkClient.active)
            {
                Debug.LogWarning("Client already started.");
                return;
            }

            mode = NetworkMode.Client;

            RegisterClientMessages();
            RegisterClientMessageListeners();

            NetworkClient.Connect(_networkAddress);

            _onStartClient?.Invoke();
        }

        public void StopServer()
        {
            if (!_serverBehaviour.isActive)
                return;

            _serverBehaviour.Shutdown();

            mode = NetworkMode.Offline;

            _onStopServer?.Invoke();
        }

        public void StopClient()
        {
            if (mode == NetworkMode.Offline)
                return;

            NetworkClient.Disconnect();

            OnClientDisconnectInternal();
        }

        public void SetNetworkAddress(string networkAddress)
        {
            if (NetworkClient.active)
            {
                throw new ArgumentException("Can't change Network Address when client already started");
            }

            if (string.IsNullOrWhiteSpace(networkAddress))
            {
                throw new ArgumentException("Network Address can't be null or empty");
            }

            if (UriHostNameType.Unknown == Uri.CheckHostName(networkAddress))
            {
                throw new ArgumentException($"Network Address format isn't correct {networkAddress}");
            }

            _networkAddress = networkAddress;
        }

        public void SetNetworkAddress(Uri networkAddress) =>
            SetNetworkAddress(networkAddress.Host);

        public string GetNetworkAddress() => _networkAddress;

        public void SendMessageToClient<T>(NetworkConnection connection, T message) where T : struct, NetworkMessage
        {
            if (mode == NetworkMode.Server)
            {
                connection.Send(message);
            }
            else
            {
                Debug.LogWarning($"Can't send message to clients because current mode is {mode.ToString()}");
            }
        }

        public void SendMessageToClient<T>(int connectionId, T message) where T : struct, NetworkMessage
        {
            SendMessageToClient(NetworkServer.connections[connectionId], message);
        }

        public void SendMessageToAllClients<T>(T message) where T : struct, NetworkMessage
        {
            if (mode == NetworkMode.Server)
            {
                NetworkServer.SendToAll(message);
            }
            else
            {
                Debug.LogWarning($"Can't send message to clients because current mode is {mode.ToString()}");
            }
        }

        public void SendMessageToServer<T>(T message) where T : struct, NetworkMessage
        {
            if (mode == NetworkMode.Client)
            {
                NetworkClient.Send(message);
            }
            else
            {
                Debug.LogWarning($"Can't send message to server because current mode is {mode.ToString()}");
            }
        }

        public IDisposable SubscribeOnStartServer(Action handler)
        {
            _onStartServer += handler;

            return new Unsubscriber(() => _onStartServer -= handler);
        }

        public IDisposable SubscribeOnStartClient(Action handler)
        {
            _onStartClient += handler;

            return new Unsubscriber(() => _onStartClient -= handler);
        }

        public IDisposable SubscribeOnStopServer(Action handler)
        {
            _onStopServer += handler;

            return new Unsubscriber(() => _onStopServer -= handler);
        }

        public IDisposable SubscribeOnStopClient(Action handler)
        {
            _onStopClient += handler;

            return new Unsubscriber(() => _onStopClient -= handler);
        }

        public IDisposable SubscribeOnServerConnect(Action<NetworkConnection> handler)
        {
            _onServerConnect += handler;

            return new Unsubscriber(() => _onServerConnect -= handler);
        }

        public IDisposable SubscribeOnClientConnect(Action handler)
        {
            _onClientConnect += handler;

            return new Unsubscriber(() => _onClientConnect -= handler);
        }

        public IDisposable SubscribeOnServerDisconnect(Action<NetworkConnection> handler)
        {
            _onServerDisconnect += handler;

            return new Unsubscriber(() => _onServerDisconnect -= handler);
        }

        public IDisposable SubscribeOnClientDisconnect(Action handler)
        {
            _onClientDisconnect += handler;

            return new Unsubscriber(() => _onClientDisconnect -= handler);
        }

        public IDisposable SubscribeOnServerError(Action<NetworkConnection, TransportError, string> handler)
        {
            _onServerError += handler;

            return new Unsubscriber(() => _onServerError -= handler);
        }

        public IDisposable SubscribeOnClientError(Action<TransportError, string> handler)
        {
            _onClientError += handler;

            return new Unsubscriber(() => _onClientError -= handler);
        }

        public IDisposable SubscribeServerOnReceiveMessage<T>(Action<NetworkConnection, T> handler) where T : struct, NetworkMessage
        {
            bool isInvokerOfTypeTRegistered = _serverMessageListeners.ContainsListenersOfType<T>();
            var unsubscriber = _serverMessageListeners.AddListener(handler);

            if (!isInvokerOfTypeTRegistered)
            {
                void RegisterHandler()
                {
                    var invoker = _serverMessageListeners.GetInvoker<T>();
                    NetworkServer.RegisterHandler(invoker);
                }
                RegisterHandler();

                if (_serverMessageListeners.TryGetListener<T>(out var listener))
                    listener.registerListenerMethod = () => RegisterHandler();
            }

            return unsubscriber;
        }

        public IDisposable SubscribeClientOnReceiveMessage<T>(Action<T> handler) where T : struct, NetworkMessage
        {
            bool isInvokerOfTypeTRegistered = _clientMessageListeners.ContainsListenersOfType<T>();
            var unsubscriber = _clientMessageListeners.AddListener(handler);

            if (!isInvokerOfTypeTRegistered)
            {
                void RegisterHandler()
                {
                    var invoker = _clientMessageListeners.GetInvoker<T>();
                    NetworkClient.RegisterHandler(invoker);
                }
                RegisterHandler();

                if (_clientMessageListeners.TryGetListener<T>(out var listener))
                    listener.registerListenerMethod = () => RegisterHandler();
            }

            return unsubscriber;
        }

        private void SetupServer()
        {
            NetworkServer.Listen(maxConnections);

            RegisterServerMessages();
            RegisterServerMessageListeners();
        }

        private void RegisterServerMessages()
        {
            NetworkServer.OnConnectedEvent = OnServerConnectInternal;
            NetworkServer.OnDisconnectedEvent = OnServerDisconnectHandler;
            NetworkServer.OnErrorEvent = OnServerErrorHandler;

            // Network Server initially registers its own handler for this, so we replace it here.
            NetworkServer.ReplaceHandler<ReadyMessage>(OnServerReadyMessageInternal);
        }

        private void RegisterClientMessages()
        {
            NetworkClient.OnConnectedEvent = OnClientConnectInternal;
            NetworkClient.OnDisconnectedEvent = OnClientDisconnectInternal;
            NetworkClient.OnErrorEvent = OnClientErrorHandler;
            NetworkClient.RegisterHandler<NotReadyMessage>(OnClientNotReadyMessageInternal);
        }

        private void RegisterClientMessageListeners()
        {
            foreach (var listener in _clientMessageListeners)
                listener.InvokeRegisterListener();
        }

        private void RegisterServerMessageListeners()
        {
            foreach (var listener in _serverMessageListeners)
                listener.InvokeRegisterListener();
        }

        private void OnServerConnectInternal(NetworkConnection conn)
        {
            conn.isAuthenticated = true;
            _onServerConnect?.Invoke(conn);
        }

        private void OnServerReadyMessageInternal(NetworkConnection conn, ReadyMessage msg) =>
            NetworkServer.SetClientReady(conn);

        private void OnClientDisconnectInternal()
        {
            if (mode == NetworkMode.Server || mode == NetworkMode.Offline)
                return;

            _onClientDisconnect?.Invoke();

            if (mode == NetworkMode.Host)
                mode = NetworkMode.Server;
            else
                mode = NetworkMode.Offline;

            NetworkClient.Shutdown();

            _onStopClient?.Invoke();
        }

        private void OnClientNotReadyMessageInternal(NotReadyMessage msg)
        {
            NetworkClient.ready = false;
        }

        private void OnServerDisconnectHandler(NetworkConnection conn) =>
            _onServerDisconnect?.Invoke(conn);

        private void OnServerErrorHandler(NetworkConnection conn, TransportError error, string reason) =>
            _onServerError?.Invoke(conn, error, reason);

        private void OnClientErrorHandler(TransportError error, string reason) =>
            _onClientError?.Invoke(error, reason);
    }*/
}
