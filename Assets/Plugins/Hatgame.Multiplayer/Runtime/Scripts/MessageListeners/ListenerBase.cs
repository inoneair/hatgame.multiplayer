
using System;

namespace Hatgame.Multiplayer
{
    public abstract class ListenerBase
    {
        public Action registerListenerMethod { get; set; }

        public void InvokeRegisterListener()
        {
            registerListenerMethod?.Invoke();
        }
    }
}
