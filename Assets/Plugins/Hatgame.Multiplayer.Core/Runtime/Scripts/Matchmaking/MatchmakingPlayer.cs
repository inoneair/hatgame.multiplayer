using System;

namespace Hatgame.Multiplayer
{
    [Serializable]
    public struct MatchmakingPlayer
    {
        public uint id;
        public string name;
        public string roomName;
    }
}
