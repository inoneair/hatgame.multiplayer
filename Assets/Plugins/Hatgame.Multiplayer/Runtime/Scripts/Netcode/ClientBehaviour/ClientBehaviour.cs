using System;
using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using Hatgame.Common;
using System.Runtime.InteropServices;

namespace Hatgame.Multiplayer
{
    public class ClientBehavior : NetworkBehaviorBase
    {
        private NativeReference<NetworkConnection> _connection;
        private NativeReference<bool> _isDisconnected;
        private UnsafeQueue<UnsafeNetworkReceivedMessage> _receivedMessages;
        private UnsafeQueue<UnsafeNetworkMessageToSend> _messagesToSend;

        private JobHandle _clientJobHandle;
        private JobHandle _clientSendHandle;

        private Action _onConnected;
        private Action _onDisconnected;
        private Action<NetworkMessageRaw> _onMessageReceived;

        private bool _isActive;

        public ushort serverPort { get; private set; }
        public string serverAddress { get; private set; }
        public override bool isActive => _isActive;

        public ClientBehavior() : base()
        {
            _connection = new NativeReference<NetworkConnection>(default, Allocator.Persistent);
            _isDisconnected = new NativeReference<bool>(true, Allocator.Persistent);
            _receivedMessages = new UnsafeQueue<UnsafeNetworkReceivedMessage>(Allocator.Persistent);
            _messagesToSend = new UnsafeQueue<UnsafeNetworkMessageToSend>(Allocator.Persistent);
        }

        public void Connect(string address, ushort port, int tickRate = 60)
        {
            if (isActive)
                return;

            if (NetworkEndpoint.TryParse(address, port, out var endpoint))
            {
                _connection.Value = _driver.Connect(endpoint);
                _isDisconnected.Value = false;
                serverAddress = address;
                serverPort = port;
                this.tickRate = tickRate;

                _tickTimeCounter.Start(0, _timeBetweenTicks, true);

                TryToCreateEventFunctionsMediator();
                MakeSubscriptionsToUnityEventFunctions();

                _isActive = true;

                _onConnected?.Invoke();
            }
            else
            {
                Debug.Log($"Endpoint {address}:{port} is not valid");
            }
        }

        public unsafe void Send(byte[] bytes)
        {
            if (!isActive)
                return;

            byte* bytesAsPtr = stackalloc byte[bytes.Length];
            Marshal.Copy(bytes, 0, (IntPtr)bytesAsPtr, bytes.Length);

            var message = new UnsafeNetworkMessageToSend
            {
                bytes = bytesAsPtr,
                numberOfBytes = bytes.Length,
                connection = _connection.Value,
            };
            _messagesToSend.Enqueue(message);
        }

        public void Disconnect()
        {
            if (!isActive)
                return;

            if (_tickTimeCounter.isActive)
                _tickTimeCounter.Stop();

            _connection.Value.Disconnect(_driver);
            _connection.Value = default;

            _isDisconnected.Value = true;

            _tickTimeCounter.Stop();
            UnsubscribeFromUnityEventFunctions();

            _clientSendHandle.Complete();
            _clientJobHandle.Complete();

            _isActive = false;

            _onDisconnected?.Invoke();
        }

        public override void Dispose()
        {
            base.Dispose();

            Disconnect();

            if (!_clientJobHandle.IsCompleted)
                _clientJobHandle.Complete();

            _connection.Dispose();
            _isDisconnected.Dispose();
            _receivedMessages.Dispose();
            _messagesToSend.Dispose();
        }

        public IDisposable SubscribeOnConnected(Action handler)
        {
            _onConnected += handler;

            return new Unsubscriber(() => _onConnected -= handler);
        }

        public IDisposable SubscribeOnDisconnected(Action handler)
        {
            _onDisconnected += handler;

            return new Unsubscriber(() => _onDisconnected -= handler);
        }

        public IDisposable SubscribeOnMessageReceived(Action<NetworkMessageRaw> handler)
        {
            _onMessageReceived += handler;

            return new Unsubscriber(() => _onMessageReceived -= handler);
        }

        protected override void TickHandle(float tickRemainder)
        {
            _clientJobHandle.Complete();

            HandleTickResults();

            if (_messagesToSend.Count > 0)
            {
                var clientSendJob = new ClientSendJob
                {
                    driver = _driver.ToConcurrent(),
                    messagesToSend = _messagesToSend.AsReadOnly(),
                };

                var sendJobHandle = clientSendJob.Schedule(_clientJobHandle);
                sendJobHandle.Complete();
                _messagesToSend.Clear();
            }

            var job = new ClientUpdateJob
            {
                driver = _driver,
                connection = _connection.Value,
                isDisconnected = _isDisconnected,
                receivedMessages = _receivedMessages,
            };
            _clientJobHandle = _driver.ScheduleUpdate();
            _clientJobHandle = job.Schedule(_clientJobHandle);
        }

        private void HandleTickResults()
        {
            while (_receivedMessages.TryDequeue(out var message))
            {
                _onMessageReceived?.Invoke(Unsafe2RawMessage(message));
                message.Dispose();
            }

            if (_isDisconnected.Value)
                Disconnect();
        }
    }
}