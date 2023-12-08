using System;
using System.Runtime.InteropServices;
using Unity.Networking.Transport;

namespace Hatgame.Multiplayer
{
    public unsafe struct UnsafeNetworkMessageToSend : IDisposable
    {
        public byte* bytes;
        public int numberOfBytes;
        public NetworkConnection connection;
        public bool sendToAll;

        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)bytes);
        }
    }
}
