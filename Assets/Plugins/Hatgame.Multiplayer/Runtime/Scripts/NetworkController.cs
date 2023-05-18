using System;
using System.Linq;
using UnityEngine;
using Mirror;

namespace Hatgame.Multiplayer
{
    public class NetworkController
    {
        public enum NetworkManagerMode { Offline, ServerOnly, ClientOnly, Host }

        static private NetworkController _instance;
        static private NetworkConnection _clientReadyConnection;

        private string _networkAddress = "localhost";

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

        private event Action _onStartHost;
        private event Action _onStartServer;
        private event Action _onStartClient;
        private event Action _onStopHost;
        private event Action _onStopServer;
        private event Action _onStopClient;

        private event Action<NetworkConnectionToClient> _onServerConnect;
        private event Action _onClientConnect;

        private event Action<NetworkConnectionToClient> _onServerDisconnect;
        private event Action _onClientDisconnect;

        private event Action<NetworkConnectionToClient, TransportError, string> _onServerError;
        private event Action<TransportError, string> _onClientError;

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

            _onStartClient?.Invoke();
        }

        public void ConnectClient()
        {
            if (!NetworkClient.active)
            {
                Debug.LogWarning("Client is not started.");
                return;
            }

            NetworkClient.Connect(_networkAddress);
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

        public void RegisterOnStartHost(Action handler)
        {
            _onStartHost += handler;
        }

        public void RegisterOnStartServer(Action handler)
        {
            _onStartServer += handler;
        }

        public void RegisterOnStartClient(Action handler)
        {
            _onStartClient += handler;
        }

        public void RegisterOnStopHost(Action handler)
        {
            _onStopHost += handler;
        }

        public void RegisterOnStopServer(Action handler)
        {
            _onStopServer += handler;
        }

        public void RegisterOnStopClient(Action handler)
        {
            _onStopClient += handler;
        }

        public void RegisterOnServerConnect(Action<NetworkConnectionToClient> handler)
        {
            _onServerConnect += handler;
        }

        public void RegisterOnClientConnect(Action handler)
        {
            _onClientConnect += handler;
        }

        public void RegisterOnServerDisconnect(Action<NetworkConnectionToClient> handler)
        {
            _onServerDisconnect += handler;
        }

        public void RegisterOnClientDisconnect(Action handler)
        {
            _onClientDisconnect += handler;
        }

        public void RegisterOnServerError(Action<NetworkConnectionToClient, TransportError, string> handler)
        {
            _onServerError += handler;
        }

        public void RegisterOnClientError(Action<TransportError, string> handler)
        {
            _onClientError += handler;
        }

        public void RegisterServerOnReceiveMessage<T>(Action<NetworkConnectionToClient, T> handler) where T : struct, NetworkMessage =>
            NetworkServer.RegisterHandler(handler);

        public void RegisterClientOnReceiveMessage<T>(Action<T> handler) where T : struct, NetworkMessage =>
            NetworkClient.RegisterHandler(handler);

        private void SetupServer()
        {
            NetworkServer.Listen(maxConnections);

            RegisterServerMessages();
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
