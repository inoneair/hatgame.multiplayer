using Mirror;

namespace Hatgame.Multiplayer
{
    public struct RequestJoinLobbyMessage : NetworkMessage
    {
        public string lobbyName;
    }
}
