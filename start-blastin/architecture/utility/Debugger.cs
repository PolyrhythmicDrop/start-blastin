using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enemies;
using Enemies.Spawners;
using Entities;
using FileIO;
using Godot;
using Interfaces;
using Items;
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

        // Key: UID, Value: Resource path
        private Dictionary<string, string> _enemyResources = new();

        private Dictionary<Node2D, Label> _entityLabels = new();

        private bool _limboConsoleOpen = false;

        // Paths and Resources

        private HashSet<StaticSpawner> _generatedSpawners = new();

        public override void _Ready()
        {
            LoadResourcePools();
            _service = ServiceManager.Instance?.GetService<PlayerService>();
            RegisterConsoleCommands();
        }

        public override void _ExitTree()
        {
            UnregisterConsoleCommands();
            base._ExitTree();
        }

        private void LoadResourcePools()
        {
            // Load items
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

            // Load enemy resources.
            HashSet<EnemyResource> enemyResources = new();
            PoolLoader.LoadResourcePool(enemyResources, "res://resources/enemies/", true);

            // Store the UID and its associated resource to the dictionary.
            foreach (EnemyResource resource in enemyResources)
            {
                string uid = ResourceUid.PathToUid(resource.ResourcePath);
                _enemyResources[uid] = resource.ResourcePath;
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
            description: "Retrieves and prints the current and base value of a stat for a given entity."
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

        [ConsoleCommand(
            description: "Sets the current value of the selected stat for a given entity."
        )]
        [AutoComplete(nameof(Entities), 0)]
        [AutoComplete(nameof(Stat), 1)]
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
            return [.. _weaponDict.Keys];
        }

        [ConsoleCommand(description: "Equips a specific plugin on the player.")]
        [AutoComplete(nameof(Plugins), 0)]
        private void EquipPlugin(string pluginName)
        {
            Player player = _service.GetPlayer(1);
            player.EquipPlugin(_pluginDict[pluginName]);
        }

        private string[] Plugins()
        {
            return [.. _pluginDict.Keys];
        }

        [ConsoleCommand(description: "Equips a specific modifier on the player.")]
        [AutoComplete(nameof(Modifier), 0)]
        private void EquipModifier(string modifierName)
        {
            Player player = _service.GetPlayer(1);
            player.EquipModifier(_modifierDict[modifierName]);
        }

        private string[] Modifier()
        {
            return [.. _modifierDict.Keys];
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

        private Label CreateEntityLabel(string entityName)
        {
            var entity = GetEntityFromName(entityName);

            if (entity is Node2D node)
            {
                Label label = new()
                {
                    Text = entityName,
                    Visible = false,
                    Position = new Vector2(0, -50),
                };
                label.AddThemeFontSizeOverride("font_size", 22);
                node.AddChild(label);
                label.Rotation = node.GlobalRotation * -1;
                _entityLabels[node] = label;
                node.TreeExiting += () => ClearEntityLabel(node);
                return label;
            }
            else
            {
                return null;
            }
        }

        private void ClearEntityLabel(Node2D node)
        {
            _entityLabels.Remove(node);
        }

        [ConsoleCommand(description: "Shows name labels for all enemies.")]
        private void ShowEntityLabels()
        {
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                if (!_entityLabels.ContainsKey(enemy))
                {
                    Label label = CreateEntityLabel(enemy.Name);
                    label.Visible = true;
                }
                else
                {
                    _entityLabels[enemy].Visible = true;
                }
            }
        }

        [ConsoleCommand(description: "Hides name labels for all enemies.")]
        private void HideEntityLabels()
        {
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                if (_entityLabels.ContainsKey(enemy))
                {
                    _entityLabels[enemy].Visible = false;
                }
            }
        }

        [ConsoleCommand("toggle_health_bars", "Turns on or off enemy health bars.")]
        private void ToggleEnemyHealthBars()
        {
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                enemy.ToggleHealthBarActive();
            }
        }

        [ConsoleCommand(
            description: "Spawns an enemy (from a UID or file path) at the given location (top, bottom, left, or right) and position (ratio along the viewport at the location)."
        )]
        [AutoComplete(nameof(EnemyResourceNames), 0)]
        [AutoComplete(nameof(SpawnerLocations), 1)]
        private async void SpawnEnemy(string enemy, string spawnerLocation, float spawnPosition)
        {
            // Convert the spawner location string to be the proper enum
            SpawnerLocation location = (SpawnerLocation)
                Enum.Parse(typeof(SpawnerLocation), spawnerLocation);

            // Convert the spawn position to be between 0 and 1.0
            spawnPosition = Math.Clamp(spawnPosition, 0, 1.0f);

            // Set names to UIDs if they're not already UID.
            if (!enemy.StartsWith("uid://"))
            {
                // Find the dictionary entry that includes the file name
                string foundPath = _enemyResources.Values.FirstOrDefault(path =>
                    path.EndsWith(enemy)
                );
                if (!string.IsNullOrEmpty(foundPath))
                {
                    enemy = ResourceUid.PathToUid(foundPath);
                }
            }

            using SpawnData data = new() { EnemyType = enemy };
            using SpawnStep step = new() { SpawnPosition = spawnPosition, Data = data };

            StaticSpawnerConfig config = new() { SpawnSteps = [step], Location = location };

            ScaleManager scaleManager = (
                (WaveManager)GetTree().GetFirstNodeInGroup("wave-manager")
            ).ScaleManager;

            Task<HashSet<EnemySpawner>> task = scaleManager.AddSpawners(config);
            await task;
            StaticSpawner spawner = (StaticSpawner)task.Result.FirstOrDefault();

            spawner.DebugMode = true;

            spawner.SpawnEnemy(step);
        }

        private string[] EnemyResourceNames()
        {
            List<string> fileNames = [];

            foreach (string path in _enemyResources.Values)
            {
                fileNames.Add(System.IO.Path.GetFileName(path));
            }

            return [.. fileNames];
        }

        private string[] SpawnerLocations()
        {
            return Enum.GetNames(typeof(SpawnerLocation));
        }
    }
}
