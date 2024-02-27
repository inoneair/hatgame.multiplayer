using Hatgame.Common;
using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace Hatgame.Multiplayer
{
    public class ServerNetworkController : IDisposable
    {
        IServerBehavior _serverBehavior;
        INetworkMessageSerializer _messageSerializer;
        private bool _isStarted;

        private Action _onStartServer;
        private Action _onStopServer;

        private Action<INetworkConnection> _onNewConnectionEstablished;
        private Action<INetworkConnection> _onClientDisconnected;

        private byte[] _sendMessagesBuffer;
        private ListenerStorage _messageListeners = new ListenerStorage();

        private int _tickRate = 60;

        private IDisposable _onOnNewConnectionEstablishedUnsubscriber;
        private IDisposable _onClientDisconnectedUnsubscriber;
        private IDisposable _onMessageReceivedUnsubscriber;

        public int tickRate => _tickRate;
        public ushort port => _serverBehavior.port;
        public int connectionsCount => _serverBehavior.connectionsCount;
        public int maxConnections => _serverBehavior.maxConnections;
        public bool isStarted => _isStarted;

        public ServerNetworkController(IServerBehavior serverBehavior, INetworkMessageSerializer messageSerializer)
        {
            _serverBehavior = serverBehavior;
            _messageSerializer = messageSerializer;

            _onOnNewConnectionEstablishedUnsubscriber = _serverBehavior.SubscribeOnNewConnectionEstablished(OnNewConnectionEstablishedHandler);
            _onClientDisconnectedUnsubscriber = _serverBehavior.SubscribeOnClientDisconnected(OnClientDisconnectedHandler);
            _onMessageReceivedUnsubscriber = _serverBehavior.SubscribeOnMessageReceived(OnMessageReceivedHandler);            
        }

        public void Dispose()
        {
            _onOnNewConnectionEstablishedUnsubscriber.Dispose();
            _onClientDisconnectedUnsubscriber.Dispose();
            _onMessageReceivedUnsubscriber.Dispose();
        }

        public void StartServer(ushort port, int tickRate = 60, int maxConnections = 16, int sendMessagesBufferSize = 1024)
        {
            if (_isStarted)
            {
                Debug.LogWarning("Server already started.");
                return;
            }

            _serverBehavior.Start(port, tickRate, maxConnections);

            _tickRate = tickRate;
            _sendMessagesBuffer = new byte[sendMessagesBufferSize];
            _isStarted = true;

            _onStartServer?.Invoke();
        }

        public void StopServer()
        {
            if (!_isStarted)
                return;

            _serverBehavior.Shutdown();

            _onStopServer?.Invoke();
        }

        public void SendMessage<T>(T message, INetworkConnection connection)
        {
            if (message == null)
                return;

            _messageSerializer.Serialize(message, ref _sendMessagesBuffer, out var numberOfBytes);
            _serverBehavior.Send(connection, _sendMessagesBuffer, numberOfBytes);
        }

        public void SendMessageToAll<T>(T message)
        {
            _messageSerializer.Serialize(message, ref _sendMessagesBuffer, out var numberOfBytes);
            _serverBehavior.SendToAll(_sendMessagesBuffer, numberOfBytes);
        }

        public IDisposable SubscribeOnStartServer(Action handler)
        {
            _onStartServer += handler;

            return new Unsubscriber(() => _onStartServer -= handler);
        }

        public IDisposable SubscribeOnStopServer(Action handler)
        {
            _onStopServer += handler;

            return new Unsubscriber(() => _onStopServer -= handler);
        }

        public IDisposable SubscribeOnNewConnectionEstablished(Action<INetworkConnection> handler)
        {
            _onNewConnectionEstablished += handler;

            return new Unsubscriber(() => _onNewConnectionEstablished -= handler);
        }

        public IDisposable SubscribeOnClientDisconnected(Action<INetworkConnection> handler)
        {
            _onClientDisconnected += handler;

            return new Unsubscriber(() => _onClientDisconnected -= handler);
        }

        public IDisposable SubscribeOnMessageReceived<T>(Action<T, INetworkConnection> handler)
        {
            var unsubscriber = _messageListeners.AddListener(handler);

            return unsubscriber;
        }

        private void OnMessageReceivedHandler(NetworkMessageRaw networkMessage, INetworkConnection connection)
        {
            var messageTypeHash = GetMessageTypeHash(networkMessage);   
            if (_messageListeners.TryGetListener(messageTypeHash, out var listener))
            {
                _messageListeners.TryGetMessageType(messageTypeHash, out var messageType);
                var message = GetMessage(networkMessage, messageType);
                listener.Invoke(message, connection);
            }
        }

        private int GetMessageTypeHash(NetworkMessageRaw networkMessage) =>
            BitConverter.ToInt32(networkMessage.bytes.AsReadOnlySpan().Slice(0, 4));

        private object GetMessage(NetworkMessageRaw networkMessage, Type messageType)
        {
            var buffer = networkMessage.bytes.AsReadOnlySpan().Slice(4);
            return DeserializeBytes(buffer, messageType);
        }

        private object DeserializeBytes(ReadOnlySpan<byte> buffer, Type messageType)
        {
            var data = Encoding.UTF8.GetString(buffer);
            var deserializedData = JsonConvert.DeserializeObject(data, messageType);
            return deserializedData;
        }

        private void OnNewConnectionEstablishedHandler(INetworkConnection connection) =>        
            _onNewConnectionEstablished?.Invoke(connection);

        private void OnClientDisconnectedHandler(INetworkConnection connection) =>
            _onClientDisconnected?.Invoke(connection);
        
    }
}
