using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using Unity.Burst;

namespace Hatgame.Multiplayer
{
    [BurstCompile]
    public struct NetworkSendToAllJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent driver;
        [ReadOnly]
        public NativeArray<NetworkConnection> connections;
        [ReadOnly]
        public NativeArray<bool> disconnected;
        public NativeArray<byte> bytes;

        public void Execute(int index)
        {
            if (connections[index].IsCreated && !disconnected[index])
            {
                driver.BeginSend(connections[index], out var dataStreamWriter);
                dataStreamWriter.WriteBytes(bytes);
                driver.EndSend(dataStreamWriter);
            }
        }
    }
}
