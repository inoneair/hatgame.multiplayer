using System;
using System.Runtime.InteropServices;
using Unity.Networking.Transport;

namespace Hatgame.Multiplayer
{
    public unsafe struct UnsafeNetworkReceivedMessage : IDisposable
    {
        public NetworkConnection connection;
        public byte* bytes;
        public int numberOfBytes;

        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)bytes);
        }
    }
}
