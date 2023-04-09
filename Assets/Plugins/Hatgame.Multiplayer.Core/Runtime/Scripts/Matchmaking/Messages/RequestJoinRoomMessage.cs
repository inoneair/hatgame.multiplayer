using Mirror;

namespace Hatgame.Multiplayer
{
    public struct RequestJoinRoomMessage : NetworkMessage
    {
        public string roomName;
    }
}
