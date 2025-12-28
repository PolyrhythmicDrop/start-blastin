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

        private SpawnerScaler _currentSpawnerScaler;
        private SpawnerFormationScaler _currentFormationScaler;

        private List<SpawnerScaler> _spawnerScalerPool = new();
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

        public SpawnerScaler CurrentSpawnerScaler => _currentSpawnerScaler;
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
            _currentSpawnerScaler = ResourceLoader.Load<SpawnerScaler>(_defaultSpawnerScaler);
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
            LoadResourcePool(_spawnerScalerPool);
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

        #region Scaling

        /// <summary>
        /// Sets the current wave scaler to a scaler from a pool.
        /// </summary>
        /// <remarks>
        /// Override this method and call <see cref="SelectScaler{T}"/> for all resource pools you need to load.
        public virtual void SetCurrentScalers(int wave)
        {
            _currentEnemyScaler = SelectScaler(_enemyScalerPool, wave, _defaultEnemyScaler);
            _currentSpawnerScaler = SelectScaler(_spawnerScalerPool, wave, _defaultSpawnerScaler);
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

        public void ScaleSpawner(EnemySpawner spawner, SpawnerConfig config)
        {
            int currentWave = _waveManager.Wave;
            float difficultyMod = _waveManager.DifficultyModifier;

            EnemyScaler adjustedEnemyScaler = _currentEnemyScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );

            SpawnerScaler adjustedSpawnerScaler = _currentSpawnerScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );

            spawner.SetEnemyScaler(adjustedEnemyScaler);
            spawner.ApplySpawnerScaling(
                currentWave,
                config.SpawnPool,
                adjustedSpawnerScaler.SpawnIntervalModifier,
                adjustedSpawnerScaler.MoveDurationModifier
            );

            EventBus.Instance.RaiseSpawnersReady();
        }

        #endregion

        #region Formations

        /// <summary>
        /// Instantiates and adds spawners to the scene based on the currently-selected formation.
        /// </summary>
        public async Task AssembleFormation()
        {
            // Clear the existing list of active spawners. We want to generate new ones from scratch each time.
            await ClearFormation();

            // Add spawners for each SpawnerConfig in the current formation.
            await AddSpawners([.. _currentFormationScaler.Formation]);
        }

        /// <summary>
        /// Clear all the EnemySpawners from each location
        /// </summary>
        /// <returns></returns>
        private async Task ClearFormation()
        {
            foreach (KeyValuePair<SpawnerLocation, List<EnemySpawner>> kvp in _activeSpawners)
            {
                foreach (EnemySpawner spawner in kvp.Value)
                {
                    spawner.QueueFree();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                kvp.Value.Clear();
            }
        }

        /// <summary>
        /// Creates new EnemySpawners based on the passed <paramref name="configs"/>
        /// and scales them according to the currently selected scalers.
        /// Adds the new spawners to the _activeSpawners list.
        /// </summary>
        /// <param name="configs">Configurations used to create and configure the new EnemySpawners.</param>
        private async Task AddSpawners(params SpawnerConfig[] configs)
        {
            // Get the scene tree and the root level node
            SceneTree tree = GetTree();
            Node levelNode = tree.GetFirstNodeInGroup("level");

            foreach (SpawnerConfig config in configs)
            {
                EnemySpawner spawner = _spawnerScene.Instantiate<EnemySpawner>();
                config.ConfigureSpawner(spawner, _waveManager.WaveTime);
                ScaleSpawner(spawner, config);

                _activeSpawners[config.Location].Add(spawner);
                levelNode.CallDeferred(MethodName.AddChild, spawner);
                await ToSignal(spawner, Node.SignalName.Ready);
            }
        }

        #endregion
    }
}
