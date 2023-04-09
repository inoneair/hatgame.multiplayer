using Mirror;

namespace Hatgame.Multiplayer
{
    public struct RequestChangePlayerNameMessage : NetworkMessage
    {
        public string newPlayerName;
    }
}
