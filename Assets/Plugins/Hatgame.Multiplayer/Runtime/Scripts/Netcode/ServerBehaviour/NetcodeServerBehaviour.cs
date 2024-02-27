using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;
using Unity.Jobs;
using UnityEngine;
using Hatgame.Common;
using System.Runtime.InteropServices;

namespace Hatgame.Multiplayer
{
    public class NetcodeServerBehavior : NetcodeNetworkBehaviorBase, IServerBehavior
    {
        private INetworkConnectionsStorage<NetworkConnection> _networkConnectionsStorage = new NetcodeConnectionsStorage();

        private NativeList<NetworkConnection> _netcodeConnections;
        private NativeArray<bool> _disconnected;
        private NativeList<NetworkConnection> _newNetcodeConnections;

        private UnsafeQueue<UnsafeNetworkReceivedMessage> _receivedMessages;
        private UnsafeQueue<UnsafeNetworkMessageToSend> _messagesToSend;

        private JobHandle _serverJobHandle;
        private JobHandle _serverSendHandle;

        private bool _isActive;

        private Action _onServerStarted;
        private Action _onServerShutdown;
        private Action<INetworkConnection> _onNewConnectionEstablished;
        private Action<INetworkConnection> _onClientDisconnected;
        private Action<NetworkMessageRaw, INetworkConnection> _onMessageReceived;

        public bool isActive => _isActive;
        public ushort port { get; private set; }
        public int maxConnections { get; private set; }
        public int connectionsCount => _netcodeConnections.Length;

        public NetcodeServerBehavior() : base()
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

            if (_netcodeConnections.IsCreated)
                _netcodeConnections.Dispose();

            if (_disconnected.IsCreated)
                _disconnected.Dispose();

            _netcodeConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
            _disconnected = new NativeArray<bool>(maxConnections, Allocator.Persistent);

            if (_newNetcodeConnections.IsCreated)
                _newNetcodeConnections.Dispose();

            _newNetcodeConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);

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

            if (_newNetcodeConnections.IsCreated)
                _newNetcodeConnections.Dispose();

            if (_disconnected.IsCreated)
                _disconnected.Dispose();

            if (_netcodeConnections.IsCreated)
                _netcodeConnections.Dispose();

            _isActive = false;

            _onServerShutdown?.Invoke();
        }

        public unsafe void Send(INetworkConnection connection, byte[] bytes, int numberOfBytes)
        {
            if (!isActive)
                return;

            if (_networkConnectionsStorage.TryGetConnectionData(connection, out var connectionData))
            {
                byte* bytesAsPtr = (byte*)Marshal.AllocHGlobal(numberOfBytes);
                Marshal.Copy(bytes, 0, (IntPtr)bytesAsPtr, numberOfBytes);

                var message = new UnsafeNetworkMessageToSend
                {
                    bytes = bytesAsPtr,
                    numberOfBytes = numberOfBytes,
                    connection = connectionData
                };
                _messagesToSend.Enqueue(message);
            }
        }

        public unsafe void SendToAll(byte[] bytes, int numberOfBytes)
        {
            if (!isActive)
                return;

            byte* bytesAsPtr = (byte*)Marshal.AllocHGlobal(numberOfBytes);
            Marshal.Copy(bytes, 0, (IntPtr)bytesAsPtr, numberOfBytes);

            var message = new UnsafeNetworkMessageToSend
            {
                bytes = bytesAsPtr,
                numberOfBytes = numberOfBytes,
                sendToAll = true
            };
            _messagesToSend.Enqueue(message);
        }

        public IDisposable SubscribeOnMessageReceived(Action<NetworkMessageRaw, INetworkConnection> handler)
        {
            _onMessageReceived += handler;

            return new Unsubscriber(() => _onMessageReceived -= handler);
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

        protected override void TickHandle(float tickRemainder)
        {
            _serverJobHandle.Complete();

            foreach (var netcodeConnection in _newNetcodeConnections)
            {
                var connection = _networkConnectionsStorage.AddConnection(netcodeConnection);
                _onNewConnectionEstablished?.Invoke(connection);
            }

            _newNetcodeConnections.Clear();

            foreach (var netcodeConnection in _netcodeConnections)
            {
                if (!netcodeConnection.IsCreated && _networkConnectionsStorage.TryRemoveConnection(netcodeConnection, out var connection))                
                    _onClientDisconnected?.Invoke(connection);                
            }

            while (_receivedMessages.TryDequeue(out var message))
            {
                if (_networkConnectionsStorage.TryGetConnection(message.connection, out var connection))
                {
                    var rawMessage = Unsafe2RawMessage(message);
                    _onMessageReceived?.Invoke(rawMessage, connection);
                }
                message.Dispose();
            }

            if (_messagesToSend.Count > 0)
            {
                var serverSendJob = new ServerSendJob
                {
                    driver = _driver.ToConcurrent(),
                    messagesToSend = _messagesToSend.AsReadOnly(),
                    connections = _netcodeConnections.AsDeferredJobArray(),
                    disconnected = _disconnected,
                };

                var sendJobHandle = serverSendJob.Schedule(_serverJobHandle);
                sendJobHandle.Complete();
                _messagesToSend.Clear();
            }

            var connectionJob = new ServerUpdateConnectionsJob
            {
                driver = _driver,
                connections = _netcodeConnections,
                newConnections = _newNetcodeConnections,
                disconnected = _disconnected,
            };

            var serverUpdateJob = new ServerUpdateJob()
            {
                driver = _driver.ToConcurrent(),
                connections = _netcodeConnections,
                receivedMessages = _receivedMessages.AsParallelWriter(),
                _disconnected = _disconnected,
            };

            _serverJobHandle = _driver.ScheduleUpdate();
            _serverJobHandle = connectionJob.Schedule(_serverJobHandle);
            _serverJobHandle = serverUpdateJob.Schedule(_netcodeConnections, 8, _serverJobHandle);
        }
    }
}
