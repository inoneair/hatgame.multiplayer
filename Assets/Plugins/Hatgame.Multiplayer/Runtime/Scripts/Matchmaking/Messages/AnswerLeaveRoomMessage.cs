using Mirror;

namespace Hatgame.Multiplayer
{
    public struct AnswerLeaveLobbyMessage : NetworkMessage
    {
        public bool isSuccess;
    }
}
