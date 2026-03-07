using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autoloads;
using Enemies.Spawners;
using Factories;
using FileIO;
using Godot;
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
        private string _defaultEnemyScaler =
            "res://resources/wave-scalers/enemy-scalers/default-enemy-scaler.tres";

        private string _defaultSpawnerScaler =
            "res://resources/wave-scalers/spawner-scalers/default-spawner-scaler.tres";
        private string _defaultFormation =
            "res://resources/wave-scalers/spawner-formations/tier1/level-one-formation.tres";

        private EnemyScaler _currentEnemyScaler;
        private EnemyScaler _previousEnemyScaler;
        private SpawnerScaler _currentSpawnerScaler;
        private SpawnerScaler _previousSpawnerScaler;

        private SpawnerFormationScaler _currentFormationScaler;
        private SpawnerFormationScaler _previousFormationScaler;

        private List<EnemyScaler> _enemyScalerPool = new();

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

        [Export(SRP_HINT.RESOURCE_PATH, "SpawnerFormationScaler")]
        public string LevelOneFormation { get; set; } = "uid://3bayun5b03eq";

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

            // Initialize enemy scalers
            _currentEnemyScaler = ResourceLoader.Load<EnemyScaler>(_defaultEnemyScaler);
            _previousEnemyScaler = _currentEnemyScaler;

            // Initialize formation scalers
            _currentFormationScaler = ResourceLoader.Load<SpawnerFormationScaler>(
                LevelOneFormation
            );
            _previousFormationScaler = _currentFormationScaler;

            // Initialize spawner scalers
            _currentSpawnerScaler = ResourceLoader.Load<SpawnerScaler>(_defaultSpawnerScaler);
            _previousSpawnerScaler = _currentSpawnerScaler;
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

                PoolLoader.LoadResourcePool(pool, directory, true);
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
            _previousEnemyScaler = _currentEnemyScaler;
            _currentEnemyScaler = SelectScaler(_enemyScalerPool, wave, _defaultEnemyScaler);

            _previousSpawnerScaler = _currentSpawnerScaler;
            _currentSpawnerScaler = SelectScaler(_spawnerScalerPool, wave, _defaultSpawnerScaler);

            _previousFormationScaler = _currentFormationScaler;
            _currentFormationScaler = SelectScaler(_formationPool, wave, _defaultFormation);

            // Log the formation for testing
            DebugLogger.LogMessage($"Formation {_currentFormationScaler.ResourceName} selected!");
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

                bool newScalerSelected = false;
                T selectedScaler = null;
                const int MAX_ATTEMPTS = 5;
                int attempts = 0;

                while (!newScalerSelected && attempts < MAX_ATTEMPTS)
                {
                    int selection = RNG.GetRandomInt(0, matchingConfigs.Count - 1);
                    selectedScaler = matchingConfigs[selection];

                    switch (typeof(T))
                    {
                        case Type t when t == typeof(EnemyScaler):
                        {
                            newScalerSelected = !selectedScaler.Equals(_previousEnemyScaler);
                            break;
                        }
                        case Type t when t == typeof(SpawnerScaler):
                        {
                            newScalerSelected = !selectedScaler.Equals(_previousSpawnerScaler);
                            break;
                        }
                        case Type t when t == typeof(SpawnerFormationScaler):
                        {
                            newScalerSelected = !selectedScaler.Equals(_previousFormationScaler);
                            break;
                        }
                    }

                    attempts++;
                }

                if (selectedScaler == null)
                {
                    throw new InvalidOperationException(
                        $"Could not find a new scaler of type {typeof(T).Name} to select! Returning default scaler."
                    );
                }
                else
                {
                    return selectedScaler;
                }

                // return matchingConfigs[selection];
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return ResourceLoader.Load<T>(defaultPath);
            }
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

            EventBus.Instance.RaiseSpawnersReady();
        }

        /// <summary>
        /// Clear all the EnemySpawners from each location
        /// </summary>
        /// <returns></returns>
        private async Task ClearFormation()
        {
            foreach (List<EnemySpawner> spawners in _activeSpawners.Values)
            {
                foreach (EnemySpawner spawner in spawners.ToList())
                {
                    spawner.QueueFree();
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                spawners.Clear();
            }
        }

        /// <summary>
        /// Creates new EnemySpawners based on the passed <paramref name="configs"/>
        /// and scales them according to the currently selected scalers.
        /// Adds the new spawners to the _activeSpawners list.
        /// </summary>
        /// <param name="configs">Configurations used to create and configure the new EnemySpawners.</param>
        public async Task<HashSet<EnemySpawner>> AddSpawners(params SpawnerConfig[] configs)
        {
            // Get the scene tree and the root level node
            SceneTree tree = GetTree();
            Node levelNode = tree.GetFirstNodeInGroup("level");
            HashSet<EnemySpawner> returnedSpawners = new();

            foreach (SpawnerConfig config in configs)
            {
                // Create the correct type of spawner based on the config.
                EnemySpawner spawner = config switch
                {
                    RandomSpawnerConfig => SpawnerFactory.CreateSpawner<RandomSpawner>(),
                    StaticSpawnerConfig => SpawnerFactory.CreateSpawner<StaticSpawner>(),
                    _ => SpawnerFactory.CreateSpawner<RandomSpawner>(),
                };

                config.ConfigureSpawner(spawner, _waveManager.WaveTime);
                ScaleSpawner(spawner, config);

                _activeSpawners[config.Location].Add(spawner);
                returnedSpawners.Add(spawner);
                levelNode.CallDeferred(MethodName.AddChild, spawner);
                await ToSignal(spawner, Node.SignalName.Ready);
            }
            return returnedSpawners;
        }

        /// <summary>
        /// Scales created spawners according to a passed <see cref="SpawnerConfig"/> object.
        /// </summary>
        /// <param name="spawner">The EnemySpawner to scale.</param>
        /// <param name="config">The configuration object to use to scale the spawner.</param>
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

            // Account for spawner config changes
            spawner.SetEnemyScaler(adjustedEnemyScaler);

            // Adjust the spawn interval and move duration if the spawner is a random one.
            if (spawner is RandomSpawner randomSpawner)
            {
                randomSpawner.ApplySpawnerScaling(
                    currentWave,
                    adjustedSpawnerScaler.SpawnIntervalModifier,
                    adjustedSpawnerScaler.MoveDurationModifier
                );
            }
        }

        #endregion
    }
}
