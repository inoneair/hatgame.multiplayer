using System;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class ListenerStorage
    { 
        protected Dictionary<int, Listener> _typedListeners = new Dictionary<int, Listener>();
        protected Dictionary<int, Type> _hashedMessageTypes = new Dictionary<int, Type>();

        public IDisposable AddListener<T, Y>(Action<T, Y> handler) where Y : INetworkConnection
        {
            var type = typeof(T);
            Listener listener = null;
            if (!_typedListeners.TryGetValue(type.GetHashCode(), out listener))
            {
                listener = new Listener();
                _typedListeners.Add(type.GetHashCode(), listener);
                _hashedMessageTypes.Add(type.GetHashCode(), type);
            }

            return listener.AddListener(handler);
        }

        public bool ContainsListenersOfType(int hash) =>
            _typedListeners.ContainsKey(hash);

        public bool ContainsListenersOfType(Type type) =>
            ContainsListenersOfType(type.GetHashCode());

        public bool ContainsListenersOfType<T>() =>
            ContainsListenersOfType(typeof(T));

        public bool TryGetListener(int hash, out Listener listener)
        {
            bool isSuccess = _typedListeners.TryGetValue(hash, out var listenerBase);
            listener = isSuccess ? listenerBase : null;
            return isSuccess;
        }

        public bool TryGetMessageType(int hash, out Type messageType) =>
            _hashedMessageTypes.TryGetValue(hash, out messageType);
    }
}
