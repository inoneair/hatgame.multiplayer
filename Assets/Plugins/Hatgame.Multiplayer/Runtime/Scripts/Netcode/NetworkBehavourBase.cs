using System;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using Hatgame.Common;

namespace Hatgame.Multiplayer
{
    public abstract class NetworkBehaviorBase : IDisposable
    {
        protected NetworkDriver _driver;

        protected Counter _tickTimeCounter = new Counter();
        private IDisposable _tickTimeUnsubscriber = null;

        private UnityEventFunctionsMediator _eventFunctionsMediator = null;
        private IDisposable _updateUnsubscriber = null;
        private IDisposable _onDestroyUnsubscriber = null;

        protected int _tickRate;
        protected float _timeBetweenTicks;

        public virtual int tickRate
        {
            get => _tickRate;
            protected set
            {
                _tickRate = value;
                _timeBetweenTicks = 1f / _tickRate;
            }
        }

        public abstract bool isActive { get; }

        public NetworkBehaviorBase()
        {
            _driver = NetworkDriver.Create();

            _tickTimeUnsubscriber = _tickTimeCounter.SubscribeOnReachTargetValue(TickHandle);
        }

        public virtual void Dispose()
        {
            if(_driver.IsCreated)
                _driver.Dispose();

            if (_eventFunctionsMediator != null && _eventFunctionsMediator.gameObject != null)
            {
                GameObject.Destroy(_eventFunctionsMediator.gameObject);
                _eventFunctionsMediator = null;
            }

            _tickTimeUnsubscriber?.Dispose();
        }

        protected abstract void TickHandle(float tickRemainder);

        private void UpdateHandle(float deltaTime)
        {
            _tickTimeCounter.AddTime(deltaTime);
        }

        protected void TryToCreateEventFunctionsMediator()
        {
            if (_eventFunctionsMediator == null || _eventFunctionsMediator.gameObject == null)
            {
                var go = new GameObject("ServerEventFunctionsMediator");
                _eventFunctionsMediator = go.AddComponent<UnityEventFunctionsMediator>();
                GameObject.DontDestroyOnLoad(go);
            }
        }

        protected void MakeSubscriptionsToUnityEventFunctions()
        {
            if (_eventFunctionsMediator == null || _eventFunctionsMediator.gameObject == null)
                throw new NullReferenceException($"Error: UnityEventFunctionsMediator is null");

            _updateUnsubscriber ??= _eventFunctionsMediator.SubscribeUpdate(UpdateHandle);
            _onDestroyUnsubscriber ??= _eventFunctionsMediator.SubscribeOnDestroy(Dispose);
        }

        protected void UnsubscribeFromUnityEventFunctions()
        {
            _updateUnsubscriber?.Dispose();
            _updateUnsubscriber = null;

            _onDestroyUnsubscriber?.Dispose();
            _onDestroyUnsubscriber = null;
        }

        protected unsafe NativeArray<byte> Ptr2NativeArray(byte* bytes, int numberOfBytes)
        {
            var arrayOfBytes = new NativeArray<byte>(numberOfBytes, Allocator.Persistent); 
            for (int i = 0; i < numberOfBytes; ++i)
                arrayOfBytes[i] = bytes[i];

            return arrayOfBytes;
        }

        protected unsafe NetworkMessageRaw Unsafe2RawMessage(UnsafeNetworkReceivedMessage unsafeMessage)
        {
            var arrayOfBytes = Ptr2NativeArray(unsafeMessage.bytes, unsafeMessage.numberOfBytes);
            return new NetworkMessageRaw { bytes = arrayOfBytes, connection = unsafeMessage.connection };
        }
    }
}
