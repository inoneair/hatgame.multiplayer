using Unity.Networking.Transport;

namespace Hatgame.Multiplayer
{
    public struct NetcodeNetworkConnection : INetworkConnection
    {
        private int _id;
        public int id => _id;

        public NetcodeNetworkConnection(int id)
        {
            _id = id;
        }
    }
}
