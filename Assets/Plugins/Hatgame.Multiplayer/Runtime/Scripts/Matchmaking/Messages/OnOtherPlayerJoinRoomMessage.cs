using Mirror;

namespace Hatgame.Multiplayer
{
    public struct OnOtherPlayerJoinLobbyMessage : NetworkMessage
    {
        public uint playerId;
        public string playerName;
    }
}
