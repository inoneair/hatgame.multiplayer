using System;
using Unity.Collections;

namespace Hatgame.Multiplayer
{
    public struct NetworkMessageRaw : IDisposable
    {
        public NativeArray<byte> bytes;

        public void Dispose()
        {
            bytes.Dispose();
        }
    }
}
