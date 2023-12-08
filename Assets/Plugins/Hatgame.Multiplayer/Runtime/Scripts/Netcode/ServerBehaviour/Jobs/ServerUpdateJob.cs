using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace Hatgame.Multiplayer
{
    public unsafe struct ServerUpdateJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent driver;
        [ReadOnly]
        public NativeList<NetworkConnection> connections;
        [WriteOnly]
        public NativeArray<bool> _disconnected;
        [WriteOnly]
        public UnsafeQueue<UnsafeNetworkReceivedMessage>.ParallelWriter receivedMessages;

        public void Execute(int index)
        {
            NetworkEvent.Type eventType;
            while ((eventType = driver.PopEventForConnection(connections[index], out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Disconnect:
                        _disconnected[index] = true;
                        break;

                    case NetworkEvent.Type.Data:
                        var bytes = (byte*)Marshal.AllocHGlobal(stream.Length);
                        stream.ReadBytesUnsafe(bytes, stream.Length);

                        var receivedMessage = new UnsafeNetworkReceivedMessage
                        {
                            connection = connections[index],
                            numberOfBytes = stream.Length,
                            bytes = bytes,
                        };
                        receivedMessages.Enqueue(receivedMessage);
                        break;
                }
            }
        }
    }
}
