using Mirror;

namespace Hatgame.Multiplayer
{
    public struct AnswerJoinRoomMessage : NetworkMessage
    {
        public bool isSuccess;
        public MatchmakingPlayer[] roomPlayers;
    }
}
