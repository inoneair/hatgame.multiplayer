using Hatgame.Common;
using System;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace Hatgame.Multiplayer
{
    public class ClientNetworkController : IDisposable
    {
        IClientBehavior _clientBehavior;
        INetworkMessageSerializer _messageSerializer;
        private bool _isStarted;

        private Action _onStartClient;
        private Action _onStopClient;

        private Action _onClientConnect;
        private Action _onClientDisconnect;

        private byte[] _sendMessagesBuffer;
        private ListenerStorage _messageListeners = new ListenerStorage();

        private IDisposable _onClientConnectUnsubscriber;
        private IDisposable _onClientDisconnectUnsubscriber;
        private IDisposable _onMessageReceivedUnsubscriber;

        public int tickRate => _clientBehavior.tickRate;
        public bool isConnected => _clientBehavior.isConnected;
        public bool isStarted => _isStarted;

        public ClientNetworkController(IClientBehavior clientBehavior, INetworkMessageSerializer messageSerializer)
        {
            _clientBehavior = clientBehavior;
            _messageSerializer = messageSerializer;

            _onClientConnectUnsubscriber = _clientBehavior.SubscribeOnConnected(OnClientConnectHandler);
            _onClientDisconnectUnsubscriber = _clientBehavior.SubscribeOnDisconnected(OnClientDisconnectHandler);
            _onMessageReceivedUnsubscriber = _clientBehavior.SubscribeOnMessageReceived(OnMessageReceivedHandler);
        }

        public void Dispose()
        {
            _onClientConnectUnsubscriber.Dispose();
            _onClientDisconnectUnsubscriber.Dispose();
            _onMessageReceivedUnsubscriber.Dispose();
        }

        public void StartClient(int sendMessagesBufferSize)
        {
            if (_isStarted)
            {
                Debug.LogWarning("Client already started.");
                return;
            }

            _sendMessagesBuffer = new byte[sendMessagesBufferSize];
            _isStarted = true;

            _onStartClient?.Invoke();
        }

        public void Connect(string address, ushort port)
        {
            if (!_isStarted)
            {
                Debug.LogWarning("Client must be started.");
                return;
            }

            _clientBehavior.Connect(address, port);
        }

        public void StopClient()
        {
            if (!_isStarted)
                return;

            if (isConnected)
                _clientBehavior.Disconnect();

            OnClientDisconnectInternal();
        }

        public void SendMessage<T>(T message)
        {
            if (message == null)
                return;

            _messageSerializer.Serialize(message, ref _sendMessagesBuffer, out var numberOfBytes);
            _clientBehavior.Send(_sendMessagesBuffer, numberOfBytes);
        }

        public IDisposable SubscribeOnStartClient(Action handler)
        {
            _onStartClient += handler;

            return new Unsubscriber(() => _onStartClient -= handler);
        }

        public IDisposable SubscribeOnStopClient(Action handler)
        {
            _onStopClient += handler;

            return new Unsubscriber(() => _onStopClient -= handler);
        }

        public IDisposable SubscribeOnConnect(Action handler)
        {
            _onClientConnect += handler;

            return new Unsubscriber(() => _onClientConnect -= handler);
        }

        public IDisposable SubscribeOnDisconnect(Action handler)
        {
            _onClientDisconnect += handler;

            return new Unsubscriber(() => _onClientDisconnect -= handler);
        }

        public IDisposable SubscribeOnMessageReceived<T>(Action<T> handler)
        {
            var unsubscriber = _messageListeners.AddListener((T message, INetworkConnection connection) => handler.Invoke(message));

            return unsubscriber;
        }

        private void OnClientDisconnectInternal()
        {
            _clientBehavior.Disconnect();

            _onStopClient?.Invoke();
        }

        private void OnMessageReceivedHandler(NetworkMessageRaw networkMessage)
        {
            var messageTypeHash = GetMessageTypeHash(networkMessage);
            if (_messageListeners.TryGetListener(messageTypeHash, out var listener))
            {
                _messageListeners.TryGetMessageType(messageTypeHash, out var messageType);
                var message = GetMessage(networkMessage, messageType);
                listener.Invoke(message);
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

        private void OnClientConnectHandler() =>
            _onClientConnect?.Invoke();

        private void OnClientDisconnectHandler() =>
            _onClientDisconnect?.Invoke();

    }
}
