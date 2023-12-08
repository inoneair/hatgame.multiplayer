using System;

namespace Hatgame.Multiplayer
{
    public class ServerListenerStorage : ListenerStorageBase
    {
        /*public IDisposable AddListener<T>(Action<NetworkConnectionToClient, T> handler) where T : struct, NetworkMessage
        {
            var type = typeof(T);
            ListenerBase listenerDataBase = null;
            ServerMessageListener<T> typedListenerData = null;
            if (_typedListeners.TryGetValue(type, out listenerDataBase))
            {
                typedListenerData = (ServerMessageListener<T>)listenerDataBase;
            }
            else
            {
                typedListenerData = new ServerMessageListener<T>();
                _typedListeners.Add(type, typedListenerData);
            }

            return typedListenerData.AddListener(handler);
        }

        public Action<NetworkConnectionToClient, T> GetInvoker<T>() where T : struct, NetworkMessage
        {
            var type = typeof(T);
            if (_typedListeners.TryGetValue(type, out var listenerDataBase))
            {
                var typedListenerData = (ServerMessageListener<T>)listenerDataBase;
                return typedListenerData.Invoke;
            }
            else return null;
        }*/
    }
}
