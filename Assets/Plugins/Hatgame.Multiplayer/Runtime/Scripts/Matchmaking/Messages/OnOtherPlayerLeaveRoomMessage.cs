using Mirror;

namespace Hatgame.Multiplayer
{
    public struct OnOtherPlayerLeaveRoomMessage : NetworkMessage
    {
        public uint playerId;
    }
}
