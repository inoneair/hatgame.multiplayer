using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Collections.LowLevel.Unsafe;

namespace Hatgame.Multiplayer
{
    public unsafe struct ServerSendJob : IJob
    {
        public NetworkDriver.Concurrent driver;
        [ReadOnly]
        public UnsafeQueue<UnsafeNetworkMessageToSend>.ReadOnly messagesToSend;
        [ReadOnly]
        public NativeArray<NetworkConnection> connections;
        [ReadOnly]
        public NativeArray<bool> disconnected;

        public void Execute()
        {
            for (int j = 0; j < messagesToSend.Count; ++j)
            {
                var message = messagesToSend[j];
                if (message.sendToAll)
                {
                    for (int i = 0; i < connections.Length; ++i)
                    {
                        SendBytes(connections[i], message.bytes, message.numberOfBytes);
                        message.Dispose();
                    }
                }
                else
                {
                    SendBytes(message.connection, message.bytes, message.numberOfBytes);
                    message.Dispose();
                }
            }
        }

        private void SendBytes(NetworkConnection connection, byte* bytes, int numberOfBytes)
        {
            driver.BeginSend(connection, out var dataStreamWriter);
            dataStreamWriter.WriteBytesUnsafe(bytes, numberOfBytes);
            driver.EndSend(dataStreamWriter);
        }
    }
}
