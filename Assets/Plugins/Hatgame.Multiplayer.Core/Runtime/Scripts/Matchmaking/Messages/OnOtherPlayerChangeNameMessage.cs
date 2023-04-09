using Mirror;

namespace Hatgame.Multiplayer
{
    public struct OnOtherPlayerChangeNameMessage : NetworkMessage
    {
        public uint playerId;
        public string newPlayerName;
    }
}
