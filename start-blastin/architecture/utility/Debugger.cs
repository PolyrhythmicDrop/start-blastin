using System;
using System.Collections.Generic;
using Enemies;
using Entities;
using FileIO;
using Godot;
using Interfaces;
using Items;
using Limbo;
using Limbo.Console.Sharp;
using Services;
using Stats;
using UI;
using WaveManagement;

namespace Utility
{
    /// <summary>
    /// Used to provide "cheats" to the developer (i.e. me) for easier testing.
    /// Add to a scene tree to gain access to its amazing powers.
    /// </summary>
    [GlobalClass]
    public partial class Debugger : Node
    {
        private PlayerService _service;

        private Dictionary<string, Plugin> _pluginDict = new();

        private Dictionary<string, WeaponPlugin> _weaponDict = new();
        private Dictionary<string, Modifier> _modifierDict = new();
        private Dictionary<string, Item> _itemDict = new();

        private bool _limboConsoleOpen = false;

        public override void _Ready()
        {
            LoadResourcePools();
            _service = ServiceManager.Instance?.GetService<PlayerService>();
            RegisterConsoleCommands();
        }

        private void LoadResourcePools()
        {
            HashSet<Item> itemSet = new();

            PoolLoader.LoadResourcePool(itemSet, "res://resources/items/", true);

            // Sort the items into the appropriate bins
            foreach (Item item in itemSet)
            {
                switch (item)
                {
                    case WeaponPlugin weaponPlugin:
                    {
                        _weaponDict[weaponPlugin.Name] = weaponPlugin;
                        break;
                    }
                    case Plugin plugin:
                    {
                        _pluginDict[plugin.Name] = plugin;
                        break;
                    }
                    case Modifier modifier:
                    {
                        _modifierDict[modifier.Name] = modifier;
                        break;
                    }
                    case Item:
                    default:
                    {
                        _itemDict[item.Name] = item;
                        break;
                    }
                }
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsPhysicalKeyPressed(Key.Quoteleft))
            {
                _limboConsoleOpen = !_limboConsoleOpen;

                if (_limboConsoleOpen)
                {
                    TogglePlayerInput(false);
                    LimboConsole.Info("Turning off player input!");
                }
                else
                {
                    TogglePlayerInput(true);
                }
                ;
            }
            if (Input.IsActionJustPressedByEvent("debug-end-wave", @event, true))
            {
                DebugEndWave();
            }
            if (Input.IsActionJustPressedByEvent("debug-add-flux", @event, true))
            {
                IncrementFlux();
            }
            if (Input.IsActionJustPressedByEvent("debug-add-bytes", @event, true))
            {
                IncrementBytes();
            }
            if (Input.IsActionJustPressedByEvent("debug-remove-flux", @event, true))
            {
                DecrementFlux();
            }
            if (Input.IsActionJustPressedByEvent("debug-remove-bytes", @event, true))
            {
                DecrementBytes();
            }
            if (Input.IsActionJustPressedByEvent("debug-heal-player", @event, true))
            {
                HealPlayerToMax();
            }
        }

        private void TogglePlayerInput(bool enable)
        {
            var players = _service.GetAllPlayers();
            foreach (Player player in players)
            {
                UiLayer.GetUiLayer(player.PlayerId).InputEnabled = enable;
                // player.Controller.Enabled = enable;
            }
        }

        [ConsoleCommand("EndWave", "Kills all enemies and ends the current wave.")]
        private void DebugEndWave()
        {
            WaveManager waveManager = GetTree().GetNodesInGroup("wave-manager")[0] as WaveManager;
            // Kill all enemies
            var enemies = EnemyFinder.GetAllEnemies();
            foreach (EnemyNode enemy in enemies)
            {
                enemy.Die();
            }
            waveManager.DebugEndWave();
        }

        private void IncrementFlux()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Flux += 100;
        }

        [ConsoleCommand("AddFlux", "Adds flux to the player.")]
        private void AddFlux(int flux)
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Flux += flux;
        }

        private void IncrementBytes()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Bytes += 100;
        }

        [ConsoleCommand("AddBytes", "Adds bytes to the player.")]
        private void AddBytes(int bytes)
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Bytes += bytes;
        }

        private void DecrementFlux()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Flux -= 100;
        }

        [ConsoleCommand("RemoveFlux", "Removes flux from the player.")]
        private void RemoveFlux(int flux)
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Flux -= flux;
        }

        private void DecrementBytes()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Bytes -= 100;
        }

        [ConsoleCommand("RemoveBytes", "Removes flux from the player.")]
        private void RemoveBytes(int bytes)
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Bytes -= bytes;
        }

        [ConsoleCommand("HealPlayerToMax", "Completely fills the player's health bar.")]
        private void HealPlayerToMax()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Heal(playerOne.MaxHealth);
        }

        [ConsoleCommand(description: "Heals an entity by a given value.")]
        [AutoComplete(nameof(Entities), 0)]
        private void HealEntity(string entityName, float value)
        {
            object entity = GetEntityFromName(entityName);
            if (entity is IHealthful healthful)
            {
                healthful.Heal(value);
            }
        }

        [ConsoleCommand(description: "Hurts an entity by a given value.")]
        [AutoComplete(nameof(Entities), 0)]
        private void HurtEntity(string entityName, float value)
        {
            object entity = GetEntityFromName(entityName);
            if (entity is IHealthful healthful)
            {
                healthful.TakeDamage(value);
            }
        }

        [ConsoleCommand(
            "GetEntityStat",
            "Retrieves and prints the current and base value of a stat for a given entity."
        )]
        [AutoComplete(nameof(Entities), 0)]
        [AutoComplete(nameof(Stat), 1)]
        private string GetEntityStat(string entityName, string stat)
        {
            object entity = GetEntityFromName(entityName);

            if (entity is IStats stats)
            {
                StatType statType = Enum.Parse<StatType>(stat);
                Stat retrievedStat = stats.GetStatManager().GetStat(statType);
                string returnString =
                    $"{entityName} {statType}:\nCurrent Value: {retrievedStat?.CurrentValue} | Base Value: {retrievedStat?.BaseValue}";
                LimboConsole.PrintLine(returnString, true);
                return returnString;
            }
            else
            {
                LimboConsole.Error($"Could not find stats for {entityName}!");
                return null;
            }
        }

        private void SetEntityStat(string entityName, string stat, float value)
        {
            object entity = GetEntityFromName(entityName);

            if (entity is IStats statful)
            {
                StatType statType = Enum.Parse<StatType>(stat);
                LimboConsole.Info(
                    $"Setting {stat} for {entityName}. Original value: {statful.GetStatManager().GetStat(statType).CurrentValue} | New value: {value}"
                );
                statful.GetStatManager().UpdateStat(statType, value);
            }
            else
            {
                LimboConsole.Error(
                    $"Could not update stat for {entityName}! Either entity was not found, or entity does not implement the {typeof(IStats).Name} interface."
                );
            }
        }

        [ConsoleCommand(description: "Retrieves and prints the current health for a given entity.")]
        [AutoComplete(nameof(Entities), 0)]
        private string GetEntityCurrentHealth(string entityName)
        {
            object entity = GetEntityFromName(entityName);
            if (entity is IHealthful healthful)
            {
                string health = $"{entityName} current health: {healthful.CurrentHealth}";
                LimboConsole.PrintLine(health);
                return health;
            }
            else
            {
                LimboConsole.PrintLine($"{entityName} has no health to print!");
                return null;
            }
        }

        [ConsoleCommand(description: "Equips a specific weapon on the player.")]
        [AutoComplete(nameof(Weapons), 0)]
        private void EquipWeapon(string weaponName)
        {
            Player player = _service.GetPlayer(1);
            player.EquipPlugin(_weaponDict[weaponName]);
        }

        private string[] Weapons()
        {
            DebugLogger.LogMessage(
                $"Plugin pool count: {_pluginDict.Count} | Weapon pool count: {_weaponDict.Count}",
                true
            );
            return [.. _weaponDict.Keys];
        }

        private object GetEntityFromName(string entityName)
        {
            object entity = null;
            // Determine the type of entity first
            if (entityName.Contains("Player"))
            {
                foreach (Player player in _service.GetAllPlayers())
                {
                    if (player.Name == entityName)
                    {
                        entity = player;
                        break;
                    }
                }
            }
            else
            {
                foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
                {
                    if (enemy.Name == entityName)
                    {
                        entity = enemy;
                        break;
                    }
                }
            }

            if (entity == null)
            {
                LimboConsole.Error($"Entity {entityName} not found!");
            }

            return entity;
        }

        private string[] Entities()
        {
            HashSet<string> entities = new();

            // Get all current enemies
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                entities.Add(enemy.Name);
            }

            // Get all players
            foreach (
                Player player in ServiceManager.Instance.GetService<PlayerService>().GetAllPlayers()
            )
            {
                entities.Add(player.Name);
            }

            return [.. entities];
        }

        private string[] Stat()
        {
            return Enum.GetNames(typeof(StatType));
        }
    }
}
