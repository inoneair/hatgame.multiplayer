using System;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    public abstract class GenericListener<T> : ListenerBase where T : Delegate
    {
        protected T _listeners;

        public IDisposable AddListener(T listener)
        {
            Delegate.Combine(_listeners, listener);

            return new Unsubscriber(() => Delegate.Remove(_listeners, listener));
        }
    }
}
