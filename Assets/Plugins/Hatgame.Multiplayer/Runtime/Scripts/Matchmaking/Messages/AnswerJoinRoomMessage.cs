using Mirror;

namespace Hatgame.Multiplayer
{
    public struct AnswerJoinLobbyMessage : NetworkMessage
    {
        public bool isSuccess;
        public MatchmakingPlayer[] players;
    }
}
