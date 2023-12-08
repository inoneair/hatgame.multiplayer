using System;
using UnityEngine;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    public class ServerBehaviourComponent : MonoBehaviour
    {
        private Action _updateHandler;
        private Action _onDestroyHandler;

        public IDisposable SetUpdateHandler(Action handler)
        {
            if (_updateHandler != null)
                throw new Exception("Handler can be setted only once");

            _updateHandler = handler;

            return new Unsubscriber(() => _updateHandler = null);
        }

        public IDisposable SetOnDestroyHandler(Action handler)
        {
            if (_onDestroyHandler != null)
                throw new Exception("Handler can be setted only once");

            _onDestroyHandler = handler;

            return new Unsubscriber(() => _onDestroyHandler = null);
        }

        private void Update()
        {
            _updateHandler?.Invoke();
        }

        private void OnDestroy()
        {
            _onDestroyHandler?.Invoke();
        }
    }
}
