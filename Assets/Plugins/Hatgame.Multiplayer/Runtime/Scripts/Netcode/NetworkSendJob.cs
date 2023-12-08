using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using Unity.Burst;

namespace Hatgame.Multiplayer
{
    [BurstCompile]
    public struct NetworkSendJob : IJob
    {
        public NetworkDriver.Concurrent driver;
        public NetworkConnection connection;
        public NativeArray<byte> bytes;

        public void Execute()
        {
            driver.BeginSend(connection, out var dataStreamWriter);
            dataStreamWriter.WriteBytes(bytes);
            driver.EndSend(dataStreamWriter);
        }
    }
}
