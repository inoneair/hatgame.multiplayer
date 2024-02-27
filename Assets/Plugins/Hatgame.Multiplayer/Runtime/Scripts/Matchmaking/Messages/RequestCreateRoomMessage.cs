using System;

namespace Hatgame.Multiplayer
{
    [Serializable]
    public struct RequestCreateLobbyMessage
    {
        public string lobbyName;
    }
}
