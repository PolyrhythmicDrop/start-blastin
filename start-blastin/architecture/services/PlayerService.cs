using System;
using System.Collections.Generic;
using System.Linq;
using Autoloads;
using Entities;
using Godot;
using Items;
using Utility;

namespace Services
{
    public partial class PlayerService : Node
    {
        /// <summary>
        /// A Dictionary of players listed by PlayerID.
        /// </summary>
        private readonly Dictionary<int, Player> _players = new();

        /// <summary>
        /// Player max health by PlayerID.
        /// </summary>
        private readonly Dictionary<int, float> _maxHealth = new();

        /// <summary>
        /// Player current health by PlayerID.
        /// </summary>
        private readonly Dictionary<int, float> _currentHealth = new();

        /// <summary>
        /// Total amount of time it takes for the player's phase ability to cool down.
        /// </summary>
        private readonly Dictionary<int, float> _phaseCooldown = new();

        /// <summary>
        /// Player's equipped plugins.
        /// </summary>
        private readonly Dictionary<int, List<Plugin>> _equippedPlugins = new();

        private readonly Dictionary<int, int> _currentFlux = new();

        private readonly Dictionary<int, int> _currentBytes = new();

        public void AddPlayerToService(Player player)
        {
            if (!_players.Values.Contains(player))
            {
                _players.TryAdd(player.PlayerId, player);
            }
        }

        public void RemovePlayerFromService(Player player)
        {
            if (_players.Values.Contains(player))
            {
                _players.Remove(player.PlayerId);
            }
        }

        public Dictionary<int, Player> GetAllPlayers()
        {
            return _players;
        }

        public Player GetPlayerByID(int id)
        {
            GD.Print("Getting Player by ID...");
            try
            {
                bool found = _players.TryGetValue(id, out Player player);
                if (found)
                {
                    DebugLogger.LogMessage(
                        $"Player found! Name: {player.Name} | ID: {player.PlayerId}",
                        true
                    );
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

        public void UpdateCurrentHealth(int id, float currentHealth)
        {
            _currentHealth[id] = currentHealth;
            DebugLogger.LogMessage(
                $"Player {id} current health updated to {_currentHealth[id]}!",
                true
            );
            EventBus.Instance.EmitSignal(
                EventBus.SignalName.PlayerCurrentHealthChanged,
                [id, currentHealth]
            );
        }

        public void UpdateMaxHealth(int id, float maxHealth)
        {
            _maxHealth[id] = maxHealth;
            DebugLogger.LogMessage($"Player {id} max health updated to {_maxHealth[id]}!", true);
            EventBus.Instance.EmitSignal(
                EventBus.SignalName.PlayerMaxHealthChanged,
                [id, maxHealth]
            );
        }

        public void UpdatePhaseCooldown(int id, float totalCooldown)
        {
            _phaseCooldown[id] = totalCooldown;
            DebugLogger.LogMessage(
                $"Player {id} total phase cooldown updated to {_phaseCooldown[id]}!",
                true
            );

            EventBus.Instance.EmitSignal(
                EventBus.SignalName.PlayerPhaseTotalCooldownChanged,
                [id, totalCooldown]
            );
        }

        public bool GetPlayerPhaseCooldown(int id, out float totalCooldown)
        {
            bool foundPhase = _phaseCooldown.TryGetValue(id, out totalCooldown);
            return foundPhase;
        }

        public bool GetPlayerHealth(int id, out float currentHealth, out float maxHealth)
        {
            bool foundMax = _maxHealth.TryGetValue(id, out maxHealth);
            bool foundCurrent = _currentHealth.TryGetValue(id, out currentHealth);

            return foundMax && foundCurrent;
        }

        public void UpdateEquippedPlugins(int id, List<Plugin> plugins)
        {
            _equippedPlugins[id] = plugins;
            DebugLogger.LogMessage($"Player {id} plugin list updated!", true);
        }

        public bool GetEquippedPlugins(int id, out List<Plugin> plugins)
        {
            bool foundPlugins = _equippedPlugins.TryGetValue(id, out plugins);

            return foundPlugins;
        }

        public bool PlayerHasPlugin(int id, Plugin plugin)
        {
            return _players[id].HasPlugin(plugin);
        }

        public bool GetPlayerCurrency(int id, out int flux, out int bytes)
        {
            bool foundFlux = _currentFlux.TryGetValue(id, out flux);
            bool foundBytes = _currentBytes.TryGetValue(id, out bytes);

            return foundFlux && foundBytes;
        }

        public void UpdatePlayerCurrency(int id, int? flux = null, int? bytes = null)
        {
            if (flux != null)
            {
                _currentFlux[id] = (int)flux;
                EventBus.Instance.EmitSignal(
                    EventBus.SignalName.PlayerFluxChange,
                    [id, _currentFlux[id]]
                );
            }

            if (bytes != null)
            {
                _currentBytes[id] = (int)bytes;
                EventBus.Instance.EmitSignal(
                    EventBus.SignalName.PlayerBytesChange,
                    [id, _currentBytes[id]]
                );
            }
        }
    }
}
