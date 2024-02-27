using System.Collections.Generic;
using Unity.Networking.Transport;

namespace Hatgame.Multiplayer
{
    public class NetcodeConnectionsStorage : INetworkConnectionsStorage<NetworkConnection>
    {
        private Dictionary<int, NetworkConnection> _connectionsData = new Dictionary<int, NetworkConnection>();
        private Dictionary<NetworkConnection, INetworkConnection> _connections = new Dictionary<NetworkConnection, INetworkConnection>();
        private int _idIterator = 0;

        public INetworkConnection AddConnection(NetworkConnection connectionData)
        {
            int id = GenerateNetworkConnectionId();
            _connectionsData.Add(id, connectionData);

            var conection = new NetcodeNetworkConnection(id);
            _connections.Add(connectionData, conection);

            return conection;
        }

        public bool TryRemoveConnection(INetworkConnection connection, out NetworkConnection connectionData)
        {
            var isRemoveSuccess = false;
            if(_connectionsData.TryGetValue(connection.id, out connectionData))
            {
                _connectionsData.Remove(connection.id);
                _connections.Remove(connectionData);
                isRemoveSuccess =  true;
            }

            return isRemoveSuccess;
        }

        public bool TryRemoveConnection(NetworkConnection connectionData, out INetworkConnection connection)
        {
            var isRemoveSuccess = false;
            if (_connections.TryGetValue(connectionData, out connection))
            {
                _connectionsData.Remove(connection.id);
                _connections.Remove(connectionData);
                isRemoveSuccess = true;
            }

            return isRemoveSuccess;
        }

        public bool TryGetConnection(NetworkConnection connectionData, out INetworkConnection connection) =>        
            _connections.TryGetValue(connectionData, out connection);        

        public bool TryGetConnectionData(INetworkConnection connection, out NetworkConnection connectionData) =>
            _connectionsData.TryGetValue(connection.id, out connectionData);

        private int GenerateNetworkConnectionId()
        {
            while(_connectionsData.ContainsKey(_idIterator))
            {
                _idIterator++;

                if (_idIterator == int.MaxValue)
                    _idIterator = 0;
            }
            
            return _idIterator;
        }
    }
}
