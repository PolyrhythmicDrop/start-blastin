using System;
using System.Collections.Generic;
using Autoloads;
using Entities;
using Events;
using Godot;
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

        public event EventHandler<PlayerIdEventArgs> RemovingPlayer;

        public PlayerService()
        {
            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerDied += OnPlayerDied;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerDied -= OnPlayerDied;
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }

        private void OnPlayerDied(object sender, PlayerDiedEventArgs args)
        {
            RemovePlayer(args.PlayerId);
            if (_players.Count == 0)
            {
                EventBus.Instance.RaiseGameOver();
            }
        }

        public void AddPlayer(Player player)
        {
            try
            {
                if (!_players.ContainsKey(player.PlayerId))
                {
                    _players.TryAdd(player.PlayerId, player);
                    DebugLogger.LogMessage($"Player added!", true);
                }
                else
                {
                    throw new ArgumentException(
                        $"{nameof(_players)} already includes this player {player}!"
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        public void RemovePlayer(Player player)
        {
            RemovePlayer(player.PlayerId);
        }

        public void RemovePlayer(int id)
        {
            try
            {
                if (_players.ContainsKey(id))
                {
                    RemovingPlayer?.Invoke(this, new PlayerIdEventArgs(id));
                    if (_players.Remove(id))
                    {
                        DebugLogger.LogMessage($"Player removed!", true);
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Could not find ID {id} in {nameof(_players)} Dictionary!",
                            paramName: nameof(id)
                        );
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"Could not find ID {id} in {nameof(_players)} Dictionary!",
                        paramName: nameof(id)
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        public void ClearPlayers()
        {
            _players.Clear();
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
