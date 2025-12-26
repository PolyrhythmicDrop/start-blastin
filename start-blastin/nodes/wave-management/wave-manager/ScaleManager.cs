using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autoloads;
using Enemies.Spawners;
using FileIO;
using Godot;
using NanoidDotNet;
using SafeResourcePicker;
using Utility;

namespace WaveManagement
{
    /// <summary>
    /// Unified scale manager that handles all scaling for a wave, including enemy, formation, and spawner scaling.
    /// </summary>
    [GlobalClass]
    public partial class ScaleManager : Node
    {
        // ~~ Enemy Scaling Variables ~~ //
        private string _defaultEnemyScaler =
            "res://resources/wave-scalers/enemy-scalers/default-enemy-scaler.tres";
        private EnemyScaler _currentEnemyScaler;
        private List<EnemyScaler> _enemyScalerPool = new();

        // ~~ Spawner Scaling Variables ~~ //

        private string _defaultSpawnerScaler =
            "res://resources/wave-scalers/spawner-scalers/default-spawner-scaler.tres";
        private string _defaultFormation =
            "res://resources/wave-scalers/spawner-formations/default-spawner-formation.tres";
        private PackedScene _spawnerScene = GD.Load<PackedScene>(
            "res://nodes/enemies/Spawners/EnemySpawner/enemy-spawner.tscn"
        );

        // private SpawnerScaler _currentSpawnerScaler;
        private SpawnerFormationScaler _currentFormationScaler;

        // private List<SpawnerScaler> _spawnerScalerPool = new();
        private List<SpawnerFormationScaler> _formationPool = new();

        // Active spawners at each spawn location.
        private Dictionary<SpawnerLocation, List<EnemySpawner>> _activeSpawners;

        protected WaveManager _waveManager;

        [Export(SRP_HINT.RESOURCE_PATH, "EnemyScaler")]
        public string DefaultEnemyScaler
        {
            get => _defaultEnemyScaler;
            set => _defaultEnemyScaler = value;
        }

        public EnemyScaler CurrentEnemyScaler => _currentEnemyScaler;

        [Export(SRP_HINT.RESOURCE_PATH, "SpawnerFormationScaler")]
        public string DefaultFormationScaler
        {
            get => _defaultFormation;
            set => _defaultFormation = value;
        }

        // public SpawnerScaler CurrentSpawnerScaler => _currentSpawnerScaler;
        public SpawnerFormationScaler CurrentFormation => _currentFormationScaler;

        #region Initialization
        public override void _Ready()
        {
            LoadResourcePools();
        }

        public virtual void Initialize(WaveManager waveManager)
        {
            _activeSpawners = new()
            {
                [SpawnerLocation.Top] = new(),
                [SpawnerLocation.Bottom] = new(),
                [SpawnerLocation.Left] = new(),
                [SpawnerLocation.Right] = new(),
            };
            _waveManager = waveManager;
            _currentEnemyScaler = ResourceLoader.Load<EnemyScaler>(_defaultEnemyScaler);
            _currentFormationScaler = ResourceLoader.Load<SpawnerFormationScaler>(
                _defaultFormation
            );
        }

        /// <summary>
        /// Loads a set of resource pools for scalers.
        /// </summary>
        /// <remarks>
        /// Override this method and call <see cref="LoadResourcePool{T}"/> for all resource pools you need to load.
        protected virtual void LoadResourcePools()
        {
            LoadResourcePool(_enemyScalerPool);
            LoadResourcePool(_formationPool);
        }

        /// <summary>
        /// Loads all the resources of type <typeparamref name="T"/> into the cache from the correct directory.
        /// Adds the loaded resources to the passed <paramref name="pool"/> of resources.
        /// </summary>
        /// <typeparam name="T">The type of resource to load. The type of resource also determines the directory to load from.</typeparam>
        /// <param name="pool">The scaler resource pool to add the loaded resource to.</param>
        protected void LoadResourcePool<T>(List<T> pool)
            where T : WaveScaler
        {
            string directory = "";
            try
            {
                if (typeof(T) == typeof(SpawnerScaler))
                {
                    directory = "res://resources/wave-scalers/spawner-scalers/";
                }
                else if (typeof(T) == typeof(SpawnerFormationScaler))
                {
                    directory = "res://resources/wave-scalers/spawner-formations/";
                }
                else if (typeof(T) == typeof(EnemyScaler))
                {
                    directory = "res://resources/wave-scalers/enemy-scalers/";
                }

                if (directory == "")
                {
                    throw new InvalidCastException(
                        $"Type {typeof(T).Name} does not have a valid resource pool for this object!"
                    );
                }

                PoolLoader.LoadResourcePool(pool, directory);
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }
        #endregion

        /// <summary>
        /// Sets the current wave scaler to a scaler from a pool.
        /// </summary>
        /// <remarks>
        /// Override this method and call <see cref="SelectScaler{T}"/> for all resource pools you need to load.
        public virtual void SetCurrentScalers(int wave)
        {
            _currentEnemyScaler = SelectScaler(_enemyScalerPool, wave, _defaultEnemyScaler);
            _currentFormationScaler = SelectScaler(_formationPool, wave, _defaultFormation);
        }

        /// <summary>
        /// Selects a scaler resource of type <typeparamref name="T"/> from the passed pool and returns it.
        /// </summary>
        /// <typeparam name="T">The type of scaler resource to find and return.</typeparam>
        /// <param name="pool">The pool to select a <typeparamref name="T"/> from.</param>
        /// <param name="wave">The current wave. Used to select an appropriate scaler.</param>
        /// <param name="defaultPath">File path of the default scaler to use in case an appropriate scaler isn't found in the pool.</param>
        /// <returns></returns>
        protected T SelectScaler<T>(List<T> pool, int wave, string defaultPath)
            where T : WaveScaler
        {
            try
            {
                List<T> matchingConfigs = pool.FindAll(config =>
                    (config.MinWave <= wave || config.MinWave == -1)
                    && (config.MaxWave >= wave || config.MaxWave == -1)
                );
                if (matchingConfigs.Count <= 0)
                {
                    throw new InvalidOperationException(
                        $"Could not find a {typeof(T).Name} that fits wave {wave} or that is set to infinite! Loading default config path..."
                    );
                }

                int selection = RNG.GetRandomInt(0, matchingConfigs.Count - 1);

                return matchingConfigs[selection];
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return ResourceLoader.Load<T>(defaultPath);
            }
        }

        public void ScaleSpawner(EnemySpawner spawner, SpawnerScaler spawnerScaler)
        {
            int currentWave = _waveManager.Wave;
            float difficultyMod = _waveManager.DifficultyModifier;

            EnemyScaler adjustedEnemyScaler = _currentEnemyScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );

            SpawnerScaler adjustedSpawnerScaler = spawnerScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );

            spawner.SetEnemyScaler(adjustedEnemyScaler);
            spawner.ApplySpawnerScaler(adjustedSpawnerScaler, currentWave);

            EventBus.Instance.RaiseSpawnersReady();

            // SpawnerScaler adjustedSpawnerScaler = _currentSpawnerScaler.GetAdjustedScaler(
            //     difficultyMod,
            //     currentWave
            // );

            // foreach (KeyValuePair<SpawnerLocation, List<EnemySpawner>> kvp in _activeSpawners)
            // {
            //     foreach (EnemySpawner spawner in kvp.Value)
            //     {
            //         spawner.SetEnemyScaler(adjustedEnemyScaler);
            //         // spawner.ApplySpawnerScaler(adjustedSpawnerScaler, _waveManager.Wave);
            //     }
            // }

            // EventBus.Instance.RaiseSpawnersReady();
        }

        #region Formations

        /// <summary>
        /// Instantiates and adds spawners to the scene based on the currently-selected formation.
        /// </summary>
        public async Task AssembleFormation()
        {
            // Clear the existing list of active spawners. We want to generate new ones from scratch each time.
            await ClearFormation();
            DebugLogger.LogMessage($"Formation cleared!", true);

            // Create spawners for each SpawnerLocation and SpawnerScaler in the current formation.
            foreach (SpawnerConfig config in _currentFormationScaler.Formation)
            {
                // Add spawners for each item in each spawner scaler list.
                await AddSpawners(config.Location, [.. config.Scalers]);
            }
            DebugLogger.LogMessage($"Formation assembled!");

            // // Get the number of spawners that should be in each location.
            // foreach (KeyValuePair<SpawnerLocation, List<EnemySpawner>> activeKvp in _activeSpawners)
            // {
            //     if (
            //         _currentFormationScaler.Formation.TryGetValue(
            //             activeKvp.Key,
            //             out var requestedQuantity
            //         )
            //     )
            //     {
            //         int activeCount = activeKvp.Value.Count;

            //         // If the selected formation scaler requests more spawners in the location than are currently active, add a new spawner to that location
            //         if (requestedQuantity > activeCount)
            //         {
            //             int quantityToAdd = requestedQuantity - activeCount;
            //             await AddSpawner(activeKvp.Key, quantityToAdd);
            //         }
            //         else if (requestedQuantity < activeCount)
            //         {
            //             int quantityToRemove = activeCount - requestedQuantity;
            //             RemoveSpawner(activeKvp.Key, quantityToRemove);
            //         }
            //     }
            //     else
            //     {
            //         // If the location doesn't exist in the current formation scaler, remove all spawners in that location, since there aren't supposed to be any.
            //         RemoveSpawner(activeKvp.Key, activeKvp.Value.Count);
            //     }
            // }
        }

        /// <summary>
        /// Clear all the EnemySpawners from each location
        /// </summary>
        /// <returns></returns>
        private async Task ClearFormation()
        {
            int count = 0;
            foreach (KeyValuePair<SpawnerLocation, List<EnemySpawner>> kvp in _activeSpawners)
            {
                foreach (EnemySpawner spawner in kvp.Value)
                {
                    spawner.QueueFree();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    count++;
                    DebugLogger.LogMessage($"Spawner {count} freed!", true);
                }
                kvp.Value.Clear();
            }
        }

        /// <summary>
        /// Adds the desired <paramref name="quantity"/> of enemy spawners to the passed <paramref name="location"/>.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="quantity"></param>
        private async Task AddSpawners(SpawnerLocation location, params SpawnerScaler[] scalers)
        {
            // if (quantity == 0)
            // {
            //     return;
            // }

            // Get the scene tree and the root level node
            SceneTree tree = GetTree();
            Node levelNode = tree.GetFirstNodeInGroup("level");

            // Set the spawner's position, size, and rotation based on the location.
            Vector2 position;
            float rotationDegrees;
            Curve2D curve;

            switch (location)
            {
                default:
                case SpawnerLocation.Top:
                    position = new Vector2(50, -82);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
                case SpawnerLocation.Left:
                    position = new Vector2(-82, 50);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Right:
                    position = new Vector2(2000, 1100);
                    rotationDegrees = 180;
                    curve = curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Bottom:
                    position = new Vector2(1870, 1162);
                    rotationDegrees = 180;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
            }

            foreach (SpawnerScaler scaler in scalers)
            {
                EnemySpawner spawner = _spawnerScene.Instantiate<EnemySpawner>();
                spawner.Name = $"{spawner.GetType().Name}-{Nanoid.Generate(size: 8)}";
                spawner.Curve = curve;
                spawner.Position = position;
                spawner.RotationDegrees = rotationDegrees;
                spawner.Location = location;
                ScaleSpawner(spawner, scaler);
                _activeSpawners[location].Add(spawner);
                levelNode.CallDeferred(MethodName.AddChild, spawner);
                await ToSignal(spawner, Node.SignalName.Ready);
            }

            // for (int i = 0; i < quantity; i++)
            // {
            //     EnemySpawner spawner = _spawnerScene.Instantiate<EnemySpawner>();
            //     spawner.Name = $"{spawner.GetType().Name}-{Nanoid.Generate(size: 8)}";
            //     spawner.Curve = curve;
            //     spawner.Position = position;
            //     spawner.RotationDegrees = rotationDegrees;
            //     spawner.Location = location;
            //     _activeSpawners[location].Add(spawner);
            //     levelNode.CallDeferred(MethodName.AddChild, spawner);
            //     await ToSignal(spawner, Node.SignalName.Ready);
            // }
        }

        // private void RemoveSpawner(SpawnerLocation location, int quantity)
        // {
        //     if (quantity == 0)
        //     {
        //         return;
        //     }
        //     // Get all the spawners at the specified location
        //     if (_activeSpawners.TryGetValue(location, out var spawnerList))
        //     {
        //         // Get a list of objects to remove using the quantity.
        //         List<EnemySpawner> removalList = spawnerList.GetRange(0, quantity);
        //         // Remove the spawners from _activeSpawners list.
        //         spawnerList.RemoveRange(0, quantity);
        //         // Free each spawner in the list of objects to remove.
        //         foreach (EnemySpawner spawner in removalList)
        //         {
        //             spawner.QueueFree();
        //         }
        //     }
        // }

        #endregion
    }
}
