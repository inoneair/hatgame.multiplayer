using System;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using UnityEngine;
using Hatgame.Common;
using System.Runtime.InteropServices;

namespace Hatgame.Multiplayer
{
    public class ServerBehavior : NetworkBehaviorBase
    {
        private NativeList<NetworkConnection> _connections;
        private NativeArray<bool> _disconnected;
        private NativeList<NetworkConnection> _newConnections;

        private UnsafeQueue<UnsafeNetworkReceivedMessage> _receivedMessages;
        private UnsafeQueue<UnsafeNetworkMessageToSend> _messagesToSend;

        private JobHandle _serverJobHandle;
        private JobHandle _serverSendHandle;

        private bool _isActive;

        private Action _onServerStarted;
        private Action _onServerShutdown;
        private Action<NetworkConnection> _onNewConnectionEstablished;
        private Action<NetworkConnection> _onDisconnected;
        private Action<NetworkMessageRaw> _onMessageReceived;

        public ushort port { get; private set; }
        public int maxConnections { get; private set; }
        public int connectionsCount => _connections.Length;
        public override bool isActive => _isActive;

        public ServerBehavior() : base()
        {
            _receivedMessages = new UnsafeQueue<UnsafeNetworkReceivedMessage>(Allocator.Persistent);
            _messagesToSend = new UnsafeQueue<UnsafeNetworkMessageToSend>(Allocator.Persistent);
        }

        public override void Dispose()
        {
            base.Dispose();

            Shutdown();

            _receivedMessages.Dispose();
            _messagesToSend.Dispose();
        }

        public void Start(ushort port, int tickRate = 60, int maxConnections = 16)
        {
            if (isActive)
                return;

            var endpoint = NetworkEndpoint.AnyIpv4;
            endpoint.Port = port;
            if (_driver.Bind(endpoint) != 0)
            {
                Debug.Log($"Failed to bind to port {port}");
                return;
            }
            else
            {
                _driver.Listen();
                _onServerStarted?.Invoke();
            }

            this.tickRate = tickRate;
            this.port = port;
            this.maxConnections = maxConnections;

            if (_connections.IsCreated)
                _connections.Dispose();

            if (_disconnected.IsCreated)
                _disconnected.Dispose();

            _connections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
            _disconnected = new NativeArray<bool>(maxConnections, Allocator.Persistent);

            if (_newConnections.IsCreated)
                _newConnections.Dispose();

            _newConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);

            _tickTimeCounter.Start(0, _timeBetweenTicks, true);

            TryToCreateEventFunctionsMediator();
            MakeSubscriptionsToUnityEventFunctions();
            _isActive = true;
        }

        public void Shutdown()
        {
            if (!isActive)
                return;

            _tickTimeCounter.Stop();
            UnsubscribeFromUnityEventFunctions();

            _serverSendHandle.Complete();
            _serverJobHandle.Complete();

            if (_newConnections.IsCreated)
                _newConnections.Dispose();

            if (_disconnected.IsCreated)
                _disconnected.Dispose();

            if (_connections.IsCreated)
                _connections.Dispose();

            _isActive = false;

            _onServerShutdown?.Invoke();
        }

        public unsafe void Send(NetworkConnection connection, byte[] bytes)
        {
            if (!isActive)
                return;
            
            byte* bytesAsPtr = (byte*)Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, (IntPtr)bytesAsPtr, bytes.Length);

            var message = new UnsafeNetworkMessageToSend
            {
                bytes = bytesAsPtr,
                numberOfBytes = bytes.Length,
                connection = connection
            };
            _messagesToSend.Enqueue(message);
        }

        public unsafe void SendToAll(byte[] bytes)
        {
            if (!isActive)
                return;

            byte* bytesAsPtr = (byte*)Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, (IntPtr)bytesAsPtr, bytes.Length);

            var message = new UnsafeNetworkMessageToSend
            {
                bytes = bytesAsPtr,
                numberOfBytes = bytes.Length,
                sendToAll = true
            };
            _messagesToSend.Enqueue(message);
        }

        public IDisposable SubscribeOnMessageReceived(Action<NetworkMessageRaw> handler)
        {
            _onMessageReceived += handler;

            return new Unsubscriber(() => _onMessageReceived -= handler);
        }

        public IDisposable SubscribeOnNewConnectionEstablished(Action<NetworkConnection> handler)
        {
            _onNewConnectionEstablished += handler;

            return new Unsubscriber(() => _onNewConnectionEstablished -= handler);
        }

        public IDisposable SubscribeOnDisconnected(Action<NetworkConnection> handler)
        {
            _onDisconnected += handler;

            return new Unsubscriber(() => _onDisconnected -= handler);
        }

        protected override void TickHandle(float tickRemainder)
        {
            _serverJobHandle.Complete();

            foreach (var connection in _newConnections)
                _onNewConnectionEstablished?.Invoke(connection);

            _newConnections.Clear();

            foreach (var connection in _connections)
            {
                if (!connection.IsCreated)
                    _onDisconnected?.Invoke(connection);
            }

            while (_receivedMessages.TryDequeue(out var message))
            {
                _onMessageReceived?.Invoke(Unsafe2RawMessage(message));
                message.Dispose();
            }

            if (_messagesToSend.Count > 0)
            {
                var serverSendJob = new ServerSendJob
                {
                    driver = _driver.ToConcurrent(),
                    messagesToSend = _messagesToSend.AsReadOnly(),
                    connections = _connections.AsDeferredJobArray(),
                    disconnected = _disconnected,
                };

                var sendJobHandle = serverSendJob.Schedule(_serverJobHandle);
                sendJobHandle.Complete();
                _messagesToSend.Clear();
            }

            var connectionJob = new ServerUpdateConnectionsJob
            {
                driver = _driver,
                connections = _connections,
                newConnections = _newConnections,
                disconnected = _disconnected,
            };

            var serverUpdateJob = new ServerUpdateJob()
            {
                driver = _driver.ToConcurrent(),
                connections = _connections,
                receivedMessages = _receivedMessages.AsParallelWriter(),
                _disconnected = _disconnected,
            };

            _serverJobHandle = _driver.ScheduleUpdate();
            _serverJobHandle = connectionJob.Schedule(_serverJobHandle);
            _serverJobHandle = serverUpdateJob.Schedule(_connections, 8, _serverJobHandle);
        }
    }

    
}
