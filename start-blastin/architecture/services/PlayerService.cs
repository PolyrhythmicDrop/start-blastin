using System;
using System.Collections.Generic;
using System.Linq;
using Autoloads;
using Entities;
using Godot;
using Items;
using UI;
using Utility;

namespace Services
{
    public partial class PlayerService : Node
    {
        /// <summary>
        /// A Dictionary of players listed by PlayerID.
        /// </summary>
        private readonly Dictionary<int, Player> _players = new();

        public IReadOnlyDictionary<int, Player> Players => _players;

        public void AddPlayer(Player player)
        {
            if (!_players.ContainsKey(player.PlayerId))
            {
                _players.TryAdd(player.PlayerId, player);
                DebugLogger.LogMessage($"Player added!", true);
            }
        }

        public void RemovePlayer(Player player)
        {
            if (_players.Remove(player.PlayerId))
            {
                DebugLogger.LogMessage($"Player removed!", true);
            }
        }

        public Player GetPlayer(int id)
        {
            try
            {
                bool found = _players.TryGetValue(id, out Player player);
                if (found)
                {
                    return player;
                }
                else
                {
                    throw new ArgumentException("Player ID not found in the player list!");
                }
            }
            catch (ArgumentException e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return null;
            }
        }

        public bool HasPlayer(int playerId) => _players.ContainsKey(playerId);

        public IEnumerable<Player> GetAllPlayers() => _players.Values;
    }
}
