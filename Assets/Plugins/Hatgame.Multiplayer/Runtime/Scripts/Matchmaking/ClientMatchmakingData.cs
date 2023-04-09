using System.Collections;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class ClientMatchmakingData
    {
        private MatchmakingPlayer _player;
        private string _currentRoom;
        private bool _isAdmin;

        private List<MatchmakingPlayer> _otherRoomPlayers = new List<MatchmakingPlayer>();

        public MatchmakingPlayer player
        {
            get => _player;
            set => _player = value;
        }

        public string currentRoom
        {
            get => _currentRoom;
            set
            {
                if (_currentRoom != value)
                {
                    _otherRoomPlayers.Clear();
                    _currentRoom = value;
                }
            }
        }

        public bool isAdmin
        {
            get => _isAdmin;
            set => _isAdmin = value;
        }

        public void AddOtherPlayerToRoom(MatchmakingPlayer player)
        {
            _otherRoomPlayers.Add(player);
        }

        public void RemoveOtherPlayerFromRoom(uint playerId)
        {
            for (int i = 0; i < _otherRoomPlayers.Count; ++i)
            {
                if (_otherRoomPlayers[i].id == playerId)
                {
                    _otherRoomPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        public void OtherPlayerChangeName(uint playerId, string newPlayerName)
        {
            for (int i = 0; i < _otherRoomPlayers.Count; ++i)
            {
                var player = _otherRoomPlayers[i];
                if (player.id == playerId)
                {
                    player.name = newPlayerName;
                    _otherRoomPlayers[i] = player;
                    break;
                }
            }
        }

        public void ClearOtherRoomPlayers() => _otherRoomPlayers.Clear();

        public void Reset()
        {
            _player = new MatchmakingPlayer();
            _currentRoom = string.Empty;
            _isAdmin = false;
            _otherRoomPlayers.Clear();
        }
    }
}
