using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Jobs;
using Unity.Burst;

namespace Hatgame.Multiplayer
{
    [BurstCompile]
    public struct ServerUpdateConnectionsJob : IJob
    {
        public NetworkDriver driver;
        public NativeList<NetworkConnection> connections;
        public NativeArray<bool> disconnected;
        [WriteOnly]
        public NativeList<NetworkConnection> newConnections;

        public void Execute()
        {
            for (int i = connections.Length - 1; i >= 0; i--)
            {
                if (disconnected[i])
                {
                    connections.RemoveAtSwapBack(i);
                    disconnected[i] = false;
                }
            }

            NetworkConnection newConnection;
            while ((newConnection = driver.Accept()) != default(NetworkConnection))
            {
                connections.Add(newConnection);
                newConnections.Add(newConnection);
            }
        }
    }
}
