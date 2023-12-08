using System;
//using Mirror;

namespace Hatgame.Multiplayer
{
    /*public class ClientListenerStorage : ListenerStorageBase
    {
        public IDisposable AddListener<T>(Action<T> handler) where T : struct, NetworkMessage
        {
            var type = typeof(T);
            ListenerBase listenerDataBase = null;
            ClientMessageListener<T> typedListenerData = null;
            if (_typedListeners.TryGetValue(type, out listenerDataBase))
            {
                typedListenerData = (ClientMessageListener<T>)listenerDataBase;
            }
            else
            {
                typedListenerData = new ClientMessageListener<T>();
                _typedListeners.Add(type, typedListenerData);
            }

            return typedListenerData.AddListener(handler);
        }

        public Action<T> GetInvoker<T>() where T : struct, NetworkMessage
        {
            var type = typeof(T);
            if (_typedListeners.TryGetValue(type, out var listenerDataBase))
            {
                var typedListenerData = (ClientMessageListener<T>)listenerDataBase;
                return typedListenerData.Invoke;
            }
            else return null;
        }
    }*/
}
