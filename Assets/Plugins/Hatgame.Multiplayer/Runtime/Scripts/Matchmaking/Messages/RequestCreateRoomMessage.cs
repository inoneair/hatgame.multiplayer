using Mirror;

namespace Hatgame.Multiplayer
{
    public struct RequestCreateRoomMessage : NetworkMessage
    {
        public string roomName;
    }
}
