using System;
using System.Collections;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public abstract class ListenerStorageBase : IEnumerable<ListenerBase>
    {
        protected Dictionary<Type, ListenerBase> _typedListeners = new Dictionary<Type, ListenerBase>();

        public bool ContainsListenersOfType(Type type) =>
            _typedListeners.ContainsKey(type);

        public bool ContainsListenersOfType<T>() =>
            ContainsListenersOfType(typeof(T));

        /*public bool TryGetListener<T>(out ListenerBase listener) where T : struct, NetworkMessage
        {
            var type = typeof(T);
            return _typedListeners.TryGetValue(type, out listener);
        }*/

        public IEnumerator<ListenerBase> GetEnumerator() =>
            _typedListeners.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _typedListeners.Values.GetEnumerator();

    }
}
