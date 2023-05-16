using System.Collections;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class MatchmakingLobby
    {
        private string _name;

        private int _maxPlayerCount;

        private List<uint> _players;

        public string name => _name;

        public int maxPlayerCount => _maxPlayerCount;

        public int playerCount => _players.Count;

        public MatchmakingLobby(string name, int maxPlayerCount)
        {
            _name = name;
            _maxPlayerCount = maxPlayerCount;
            _players = new List<uint>(_maxPlayerCount);
        }

        public bool AddPlayer(uint playerId)
        {
            if (_players.Count == _maxPlayerCount)
                return false;

            if (_players.Contains(playerId))
                return false;

            _players.Add(playerId);
            return true;
        }

        public bool RemovePlayer(uint playerId) => _players.Remove(playerId);

        public uint[] GetPlayers() => _players.ToArray();

        public uint GetAdminId() => _players.Count <= 0 ? 0 : _players[0];
    }
}
