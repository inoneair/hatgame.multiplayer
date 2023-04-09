using Mirror;

namespace Hatgame.Multiplayer
{
    public struct OnOtherPlayerJoinRoomMessage : NetworkMessage
    {
        public uint playerId;
        public string playerName;
    }
}
