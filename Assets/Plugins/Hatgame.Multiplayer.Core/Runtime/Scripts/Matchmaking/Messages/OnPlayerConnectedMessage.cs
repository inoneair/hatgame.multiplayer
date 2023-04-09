using Mirror;

namespace Hatgame.Multiplayer
{
    public struct OnPlayerConnectedMessage : NetworkMessage
    {
        public MatchmakingPlayer player;
    }
}
