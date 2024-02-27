using System;

namespace Hatgame.Multiplayer
{
    public interface IClientBehavior : IDisposable
    {
        ushort serverPort { get; }
        string serverAddress { get; }
        bool isConnected { get; }
        int tickRate { get; }

        void Connect(string address, ushort port, int tickRate = 60);
        void Send(byte[] bytes, int numberOfBytes);
        void Disconnect();

        public IDisposable SubscribeOnConnected(Action handler);
        public IDisposable SubscribeOnDisconnected(Action handler);
        public IDisposable SubscribeOnMessageReceived(Action<NetworkMessageRaw> handler);
    }
}
