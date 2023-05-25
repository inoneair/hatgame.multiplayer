using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    public class NetworkController
    {
        public enum NetworkManagerMode { Offline, ServerOnly, ClientOnly, Host }

        static private NetworkController _instance;
        static private NetworkConnection _clientReadyConnection;

        private string _networkAddress = "localhost";

        private Action _onStartHost;
        private Action _onStartServer;
        private Action _onStartClient;
        private Action _onStopHost;
        private Action _onStopServer;
        private Action _onStopClient;

        private Action<NetworkConnectionToClient> _onServerConnect;
        private Action _onClientConnect;

        private Action<NetworkConnectionToClient> _onServerDisconnect;
        private Action _onClientDisconnect;

        private Action<NetworkConnectionToClient, TransportError, string> _onServerError;
        private Action<TransportError, string> _onClientError;

        private ServerListenerStorage _serverMessageListeners = new ServerListenerStorage();
        private ClientListenerStorage _clientMessageListeners = new ClientListenerStorage();

        public int serverTickRate
        {
            get => NetworkServer.tickRate;
            set => NetworkServer.tickRate = value;
        }

        public int maxConnections
        {
            get => NetworkServer.maxConnections;
            set => NetworkServer.maxConnections = Mathf.Max(value, 0);
        }

        public Transport transport
        {
            get => Transport.active;
            set
            {
                if (NetworkServer.active || NetworkServer.active)
                {
                    throw new ArgumentException("Can't change transport when network is active");
                }

                Transport.active = value;
            }
        }

        public static NetworkController instance => _instance ??= new NetworkController();

        public int connectionsCount => NetworkServer.connections.Count(kv => kv.Value.identity != null);

        public bool isNetworkActive => NetworkServer.active || NetworkClient.active;

        public NetworkManagerMode mode { get; private set; }

        private NetworkController()
        {
            serverTickRate = 30;
        }

        public void StartServer()
        {
            if (NetworkServer.active)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

            mode = NetworkManagerMode.ServerOnly;

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

            mode = NetworkManagerMode.ClientOnly;

            RegisterClientMessages();
            RegisterClientMessageListeners();

            NetworkClient.Connect(_networkAddress);

            _onStartClient?.Invoke();
        }

        public void StartHost()
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                Debug.LogWarning("Server or Client already started.");
                return;
            }

            mode = NetworkManagerMode.Host;

            SetupServer();

            SetNetworkAddress("localhost");

            NetworkClient.ConnectHost();
            RegisterClientMessages();
            HostMode.InvokeOnConnected();

            _onStartServer?.Invoke();
            _onStartClient?.Invoke();
            _onStartHost?.Invoke();
        }

        public void StopHost()
        {
            StopClient();
            StopServer();

            _onStopHost?.Invoke();
        }

        public void StopServer()
        {
            if (!NetworkServer.active)
                return;

            NetworkServer.Shutdown();

            mode = NetworkManagerMode.Offline;

            _onStopServer?.Invoke();
        }

        public void StopClient()
        {
            if (mode == NetworkManagerMode.Offline)
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

        public void SendMessageToClient<T>(NetworkConnectionToClient connection, T message) where T : struct, NetworkMessage
        {
            if (mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ServerOnly)
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
            if (mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ServerOnly)
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
            if (mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ClientOnly)
            {
                NetworkClient.Send(message);
            }
            else
            {
                Debug.LogWarning($"Can't send message to server because current mode is {mode.ToString()}");
            }
        }

        public IDisposable SubscribeOnStartHost(Action handler)
        {
            _onStartHost += handler;

            return new Unsubscriber(() => _onStartHost -= handler);
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

        public IDisposable SubscribeOnStopHost(Action handler)
        {
            _onStopHost += handler;

            return new Unsubscriber(() => _onStopHost -= handler);
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

        public IDisposable SubscribeOnServerConnect(Action<NetworkConnectionToClient> handler)
        {
            _onServerConnect += handler;

            return new Unsubscriber(() => _onServerConnect -= handler);
        }

        public IDisposable SubscribeOnClientConnect(Action handler)
        {
            _onClientConnect += handler;

            return new Unsubscriber(() => _onClientConnect -= handler);
        }

        public IDisposable SubscribeOnServerDisconnect(Action<NetworkConnectionToClient> handler)
        {
            _onServerDisconnect += handler;

            return new Unsubscriber(() => _onServerDisconnect -= handler);
        }

        public IDisposable SubscribeOnClientDisconnect(Action handler)
        {
            _onClientDisconnect += handler;

            return new Unsubscriber(() => _onClientDisconnect -= handler);
        }

        public IDisposable SubscribeOnServerError(Action<NetworkConnectionToClient, TransportError, string> handler)
        {
            _onServerError += handler;

            return new Unsubscriber(() => _onServerError -= handler);
        }

        public IDisposable SubscribeOnClientError(Action<TransportError, string> handler)
        {
            _onClientError += handler;

            return new Unsubscriber(() => _onClientError -= handler);
        }

        public IDisposable SubscribeServerOnReceiveMessage<T>(Action<NetworkConnectionToClient, T> handler) where T : struct, NetworkMessage
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

        private void OnServerConnectInternal(NetworkConnectionToClient conn)
        {
            conn.isAuthenticated = true;
            _onServerConnect?.Invoke(conn);
        }

        private void OnServerReadyMessageInternal(NetworkConnectionToClient conn, ReadyMessage msg) =>
            NetworkServer.SetClientReady(conn);

        private void OnClientConnectInternal() =>
            OnClientAuthenticated();

        private void OnClientAuthenticated()
        {
            NetworkClient.connection.isAuthenticated = true;

            _clientReadyConnection = NetworkClient.connection;

            if (!NetworkClient.ready)
                NetworkClient.Ready();

            _onClientConnect?.Invoke();
        }

        private void OnClientDisconnectInternal()
        {
            if (mode == NetworkManagerMode.ServerOnly || mode == NetworkManagerMode.Offline)
                return;

            _onClientDisconnect?.Invoke();

            if (mode == NetworkManagerMode.Host)
                mode = NetworkManagerMode.ServerOnly;
            else
                mode = NetworkManagerMode.Offline;

            NetworkClient.Shutdown();

            _onStopClient?.Invoke();
        }

        private void OnClientNotReadyMessageInternal(NotReadyMessage msg)
        {
            NetworkClient.ready = false;
        }

        private void OnServerDisconnectHandler(NetworkConnectionToClient conn) =>
            _onServerDisconnect?.Invoke(conn);

        private void OnServerErrorHandler(NetworkConnectionToClient conn, TransportError error, string reason) =>
            _onServerError?.Invoke(conn, error, reason);

        private void OnClientErrorHandler(TransportError error, string reason) =>
            _onClientError?.Invoke(error, reason);
    }
}
