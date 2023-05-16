using System.Collections;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class ClientMatchmakingData
    {
        private MatchmakingPlayer _player;
        private string _currentLobby;
        private bool _isAdmin;

        private List<MatchmakingPlayer> _otherLobbyPlayers = new List<MatchmakingPlayer>();

        public MatchmakingPlayer player
        {
            get => _player;
            set => _player = value;
        }

        public string currentLobby
        {
            get => _currentLobby;
            set
            {
                if (_currentLobby != value)
                {
                    _otherLobbyPlayers.Clear();
                    _currentLobby = value;
                }
            }
        }

        public bool isAdmin
        {
            get => _isAdmin;
            set => _isAdmin = value;
        }

        public void AddOtherPlayerToLobby(MatchmakingPlayer player)
        {
            _otherLobbyPlayers.Add(player);
        }

        public void RemoveOtherPlayerFromLobby(uint playerId)
        {
            for (int i = 0; i < _otherLobbyPlayers.Count; ++i)
            {
                if (_otherLobbyPlayers[i].id == playerId)
                {
                    _otherLobbyPlayers.RemoveAt(i);
                    break;
                }
            }
        }

        public void OtherPlayerChangeName(uint playerId, string newPlayerName)
        {
            for (int i = 0; i < _otherLobbyPlayers.Count; ++i)
            {
                var player = _otherLobbyPlayers[i];
                if (player.id == playerId)
                {
                    player.name = newPlayerName;
                    _otherLobbyPlayers[i] = player;
                    break;
                }
            }
        }

        public void ClearOtherLobbyPlayers() => _otherLobbyPlayers.Clear();

        public void Reset()
        {
            _player = new MatchmakingPlayer();
            _currentLobby = string.Empty;
            _isAdmin = false;
            _otherLobbyPlayers.Clear();
        }
    }
}
