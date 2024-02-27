
namespace Hatgame.Multiplayer
{
    public interface INetworkConnectionsStorage<T>
    {
        INetworkConnection AddConnection(T connectionData);        
        bool TryRemoveConnection(INetworkConnection connection, out T connectionData);
        bool TryRemoveConnection(T connectionData, out INetworkConnection connection);
        bool TryGetConnection(T connectionData, out INetworkConnection connection);
        bool TryGetConnectionData(INetworkConnection connection, out T connectionData);
    }
}
