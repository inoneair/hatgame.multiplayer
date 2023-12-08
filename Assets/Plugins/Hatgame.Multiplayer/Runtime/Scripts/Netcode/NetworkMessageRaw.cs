using System;
using Unity.Collections;
using Unity.Networking.Transport;

namespace Hatgame.Multiplayer
{
    public struct NetworkMessageRaw : IDisposable
    {
        public NetworkConnection connection;
        public NativeArray<byte> bytes;

        public void Dispose()
        {
            bytes.Dispose();
        }
    }
}
