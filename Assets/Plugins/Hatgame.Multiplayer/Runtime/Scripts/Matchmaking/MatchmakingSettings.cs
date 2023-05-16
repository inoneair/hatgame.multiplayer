using System;

namespace Hatgame.Multiplayer
{
    [Serializable]
    public struct MatchmakingSettings
    {
        public int maxLobbyCount;
        public int maxPlayersPerLobby;
    }
}
