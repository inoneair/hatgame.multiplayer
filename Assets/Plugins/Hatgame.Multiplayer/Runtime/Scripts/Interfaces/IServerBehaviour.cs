using System;

namespace Hatgame.Multiplayer
{
    public interface IServerBehavior : IDisposable
    {
        bool isActive { get; }
        ushort port { get; }
        int maxConnections { get; }
        int connectionsCount { get; }

        void Start(ushort port, int tickRate, int maxConnections);
        void Shutdown();

        void Send(INetworkConnection connection, byte[] bytes, int numberOfBytes);
        void SendToAll(byte[] bytes, int numberOfBytes);

        IDisposable SubscribeOnMessageReceived(Action<NetworkMessageRaw, INetworkConnection> handler);

        IDisposable SubscribeOnNewConnectionEstablished(Action<INetworkConnection> handler);

        public IDisposable SubscribeOnClientDisconnected(Action<INetworkConnection> handler);
        
    }
}
