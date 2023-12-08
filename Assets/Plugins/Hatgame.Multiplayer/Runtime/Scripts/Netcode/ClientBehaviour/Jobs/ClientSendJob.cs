using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport;

namespace Hatgame.Multiplayer
{
    public unsafe struct ClientSendJob : IJob
    {
        public NetworkDriver.Concurrent driver;
        [ReadOnly]
        public UnsafeQueue<UnsafeNetworkMessageToSend>.ReadOnly messagesToSend;

        public void Execute()
        {
            for (int j = 0; j < messagesToSend.Count; ++j)
            {
                var message = messagesToSend[j];
                SendMessage(message.connection, message.bytes, message.numberOfBytes);
            }
        }

        private void SendMessage(NetworkConnection connection, byte* bytes, int numberOfBytes)
        {
            driver.BeginSend(connection, out var dataStreamWriter);
            dataStreamWriter.WriteBytesUnsafe(bytes, numberOfBytes);
            driver.EndSend(dataStreamWriter);
        }
    }
}
