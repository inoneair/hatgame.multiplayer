using Mirror;

namespace Hatgame.Multiplayer
{
    public struct RequestCreateLobbyMessage : NetworkMessage
    {
        public string lobbyName;
    }
}
