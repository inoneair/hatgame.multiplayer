using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace Hatgame.Multiplayer
{
    public unsafe struct ClientUpdateJob : IJob
    {
        public NetworkDriver driver;
        public NetworkConnection connection;
        [WriteOnly]
        public UnsafeQueue<UnsafeNetworkReceivedMessage> receivedMessages;
        [WriteOnly]
        public NativeReference<bool> isDisconnected;

        public void Execute()
        {
            NetworkEvent.Type eventType;
            while ((eventType = driver.PopEventForConnection(connection, out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Connect:
                        break;

                    case NetworkEvent.Type.Data:
                        byte* bytes = (byte*)Marshal.AllocHGlobal(stream.Length);
                        var receivedMessage = new UnsafeNetworkReceivedMessage
                        {
                            connection = connection,
                            numberOfBytes = stream.Length,
                            bytes = bytes
                        };
                        stream.ReadBytesUnsafe(receivedMessage.bytes, stream.Length);
                        receivedMessages.Enqueue(receivedMessage);
                        break;

                    case NetworkEvent.Type.Disconnect:
                        isDisconnected.Value = true;
                        break;
                }
            }
        }
    }
}
