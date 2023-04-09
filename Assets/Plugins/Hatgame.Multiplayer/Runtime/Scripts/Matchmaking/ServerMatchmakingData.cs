using System.Collections;
using System.Collections.Generic;

namespace Hatgame.Multiplayer
{
    public class ServerMatchmakingData
    {
        private int _maxPlayersPreRoom;
        private int _maxRoomCount;

        private uint _lastPlayerId = 1;

        private Dictionary<string, MatchmakingRoom> _rooms = new Dictionary<string, MatchmakingRoom>();
        private Dictionary<uint, MatchmakingPlayer> _players = new Dictionary<uint, MatchmakingPlayer>();

        public ServerMatchmakingData(int maxPlayersPerRoom, int maxRoomCount)
        {
            _maxPlayersPreRoom = maxPlayersPerRoom;
            _maxRoomCount = maxRoomCount;
        }

        public bool CreateRoom(string roomName, uint playerId)
        {
            if (_rooms.Count == _maxRoomCount)
                return false;

            if (_rooms.ContainsKey(roomName))
                return false;

            if (!_players.ContainsKey(playerId))
                return false;

            var matchmakingRoom = new MatchmakingRoom(roomName, _maxPlayersPreRoom);
            _rooms.Add(roomName, matchmakingRoom);

            return true;
        }

        public uint[] GetRoomPlayers(string roomName) =>
            _rooms.TryGetValue(roomName, out var room) ? room.GetPlayers() : null;

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
                RemovePlayerFromRoom(playerId);
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

        public bool AddPlayerToRoom(uint playerId, string roomName)
        {
            if (!_rooms.ContainsKey(roomName))
                return false;

            if (!_players.ContainsKey(playerId))
                return false;

            var result = _rooms[roomName].AddPlayer(playerId);
            if (result)
            {
                var player = _players[playerId];

                RemovePlayerFromRoom(playerId);

                player.roomName = roomName;
                _players[playerId] = player;
            }

            return result;
        }

        public bool RemovePlayerFromRoom(uint playerId)
        {
            if (_players.TryGetValue(playerId, out var player))
            {
                if (_rooms.TryGetValue(player.roomName, out var room))
                {
                    if (room.RemovePlayer(playerId))
                    {
                        if (room.playerCount == 0)
                            _rooms.Remove(player.roomName);
                    }

                    player.roomName = string.Empty;
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

        public bool TryGetRoomAdmin(string roomName, out uint playerId)
        {
            if (_rooms.TryGetValue(roomName, out var room))
            {
                playerId = room.GetAdminId();
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
