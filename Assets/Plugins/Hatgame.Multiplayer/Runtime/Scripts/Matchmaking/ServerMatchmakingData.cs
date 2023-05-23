using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class ServerMatchmakingData
    {
        private int _maxPlayersPerLobby = 2;
        private int _maxLobbyCount = 10;

        private uint _lastPlayerId = 1;

        private Dictionary<string, MatchmakingLobby> _lobbies = new Dictionary<string, MatchmakingLobby>();
        private Dictionary<uint, MatchmakingPlayer> _players = new Dictionary<uint, MatchmakingPlayer>();

        public int maxPlayersPerLobby
        {
            get => _maxPlayersPerLobby;
            set => _maxPlayersPerLobby = value;
        }

        public int maxLobbyCount
        {
            get => _maxLobbyCount;
            set => _maxLobbyCount = value;
        }

        public bool CreateLobby(string lobbyName, uint playerId)
        {
            if (_lobbies.Count == _maxLobbyCount)
                return false;

            if (_lobbies.ContainsKey(lobbyName))
                return false;

            if (!_players.ContainsKey(playerId))
                return false;

            var matchmakingLobby = new MatchmakingLobby(lobbyName, _maxPlayersPerLobby);
            _lobbies.Add(lobbyName, matchmakingLobby);

            return true;
        }

        public uint[] GetLobbyPlayers(string lobbyName) =>
            _lobbies.TryGetValue(lobbyName, out var lobby) ? lobby.GetPlayers() : null;

        public MatchmakingPlayer AddPlayer(string playerName)
        {
            var player = new MatchmakingPlayer { id = GeneratePlayerId(), name = playerName };
            _players.Add(player.id, player);
            return player;
        }

        public MatchmakingPlayer? GetPlayer(uint playerId) =>
            _players.TryGetValue(playerId, out var player) ? player : null;

        public bool RemovePlayer(uint playerId)
        {
            if (_players.ContainsKey(playerId))
            {
                var player = _players[playerId];
                RemovePlayerFromLobby(playerId);
                _players.Remove(playerId);

                return true;
            }

            return false;
        }

        public bool ChangePlayerName(uint playerId, string newPlayerName)
        {
            if (_players.ContainsKey(playerId))
            {
                var player = _players[playerId];
                player.name = newPlayerName;
                _players[playerId] = player;

                return true;
            }

            return false;
        }

        public bool AddPlayerToLobby(uint playerId, string lobbyName)
        {
            if (!_lobbies.ContainsKey(lobbyName))
                return false;

            if (!_players.ContainsKey(playerId))
                return false;

            var result = _lobbies[lobbyName].AddPlayer(playerId);
            if (result)
            {
                var player = _players[playerId];

                RemovePlayerFromLobby(playerId);

                player.lobbyName = lobbyName;
                _players[playerId] = player;
            }

            return result;
        }

        public bool RemovePlayerFromLobby(uint playerId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                if (_lobbies.TryGetValue(player.lobbyName, out var lobby))
                {
                    if (lobby.RemovePlayer(playerId))
                    {
                        if (lobby.playerCount == 0)
                            _lobbies.Remove(player.lobbyName);
                    }

                    player.lobbyName = string.Empty;
                    _players[playerId] = player;

                    return true;
                }
            }

            return false;
        }

        public bool IsAdmin(uint playerId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {

            }

            return false;
        }

        public bool TryGetLobbyAdmin(string lobbyName, out uint playerId)
        {
            if (_lobbies.TryGetValue(lobbyName, out var lobby))
            {
                playerId = lobby.GetAdminId();
                return true;
            }
            else
            {
                playerId = 0;
                return false;
            }
        }

        private uint GeneratePlayerId()
        {
            while (_players.ContainsKey(_lastPlayerId))
            {
                if (_lastPlayerId == uint.MaxValue)
                    _lastPlayerId = 1;
                else
                    ++_lastPlayerId;
            }
            return _lastPlayerId;
        }
    }
}
