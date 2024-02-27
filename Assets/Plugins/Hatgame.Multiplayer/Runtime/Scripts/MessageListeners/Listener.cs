using System;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    /*public class Listener
    {
        protected MulticastDelegate _listeners;

        public IDisposable AddListener(Delegate listener)
        {
            Delegate.Combine(_listeners, listener);

            return new Unsubscriber(() => Delegate.Remove(_listeners, listener));
        }

        public IDisposable AddListener(Action<object> listener)
        {
            Delegate.Combine(_listeners, listener);

            return new Unsubscriber(() => Delegate.Remove(_listeners, listener));
        }

        public void Invoke(params object[] args)
        {
            if(_listeners != null)
                (_listeners)(args);
        }
    }*/

    public class Listener
    {
        protected Action<object> _oneArgHandlers;
        protected Action<object, object> _twoArgsHandlers;

        public IDisposable AddListener<T>(Action<T> handler)
        {
            Action<object> wrapped = (arg) => handler?.Invoke((T)arg);
            _oneArgHandlers += wrapped;

            return new Unsubscriber(() => _oneArgHandlers -= wrapped);
        }

        public IDisposable AddListener<T, Y>(Action<T, Y> handler)
        {
            Action<object, object> wrapped = (arg1, arg2) => handler?.Invoke((T)arg1, (Y)arg2);
            _twoArgsHandlers += wrapped;

            return new Unsubscriber(() => _twoArgsHandlers -= wrapped);
        }

        public void Invoke(object arg)
        {
            _oneArgHandlers?.Invoke(arg);
        }

        public void Invoke(object arg1, object arg2)
        {
            _twoArgsHandlers?.Invoke(arg1, arg2);
        }
    }
}
