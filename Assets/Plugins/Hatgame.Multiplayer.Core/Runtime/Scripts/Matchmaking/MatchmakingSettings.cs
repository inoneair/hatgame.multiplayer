using System;

namespace Hatgame.Multiplayer
{
    [Serializable]
    public struct MatchmakingSettings
    {
        public int maxRoomCount;
        public int maxPlayersPerRoom;
    }
}
