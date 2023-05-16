using Mirror;

namespace Hatgame.Multiplayer
{
    public struct OnOtherPlayerLeaveLobbyMessage : NetworkMessage
    {
        public uint playerId;
    }
}
