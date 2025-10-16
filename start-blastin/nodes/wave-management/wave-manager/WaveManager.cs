using System;
using System.Collections.Generic;
using System.Reflection;
using Enemies;
using Godot;
using SafeResourcePicker;

namespace WaveManagement
{
    [GlobalClass]
    public partial class WaveManager : Node
    {
        private int _wave = 1;
        private float _difficultyModifier = 0.1f;
        private Timer _waveTimer;
        private double _waveTime;
        private EnemyScaleManager _enemyScaleManager;

        // private string _defaultEnemyScaler;
        // private EnemyScaler _currentEnemyScaler;
        // private List<EnemyScaler> _enemyScalerPool = new();

        public int Wave => _wave;

        /// <summary>
        /// Player-selected difficulty. Adjusts the difficulty modifier, which affects per-wave stat scaling.
        /// </summary>
        [Export]
        public Difficulty Difficulty { get; set; } = Difficulty.Easy;

        [Export]
        public double WaveTime
        {
            get => _waveTime;
            set => _waveTime = value;
        }

        // [Export(SRP_HINT.RESOURCE_PATH, "EnemyScaler")]
        // public string DefaultEnemyScaler
        // {
        //     get => _defaultEnemyScaler;
        //     set => _defaultEnemyScaler = value;
        // }

        [Signal]
        public delegate void WaveStartedEventHandler();

        [Signal]
        public delegate void WaveEndedEventHandler();

        #region Initialization

        public override void _Ready()
        {
            _waveTimer = GetNode<Timer>("%WaveTimer");
            _waveTimer.Timeout += EndWave;
            _waveTimer.WaitTime = _waveTime;

            _enemyScaleManager = GetNode<EnemyScaleManager>("%EnemyScaleManager");
            _enemyScaleManager.Initialize(this);

            SetBaseDifficultyModifier();
            // LoadConfigPools();
            SetScalers();

            // If there are any spawners currently in the scene, connect their spawn timer to the WaveManager to start and stop spawning.
            var spawners = GetTree().GetNodesInGroup("enemy-spawners");
            foreach (EnemySpawner spawner in spawners)
            {
                WaveStarted += () => spawner.ToggleSpawning(true);
                WaveEnded += () => spawner.ToggleSpawning(false);
            }
            ScaleSpawners();

            StartWave();
        }

        /// <summary>
        /// Sets the base difficulty modifier based on the player's selected difficulty level.
        /// Difficulty modifier is applied to all enemy stat scaling.
        /// </summary>
        private void SetBaseDifficultyModifier()
        {
            switch (Difficulty)
            {
                case Difficulty.Easy:
                    _difficultyModifier = 0.1f;
                    break;
                default:
                case Difficulty.Medium:
                    _difficultyModifier = 0.2f;
                    break;
                case Difficulty.Hard:
                    _difficultyModifier = 0.3f;
                    break;
            }
            GD.Print($"Base difficulty modifier set: {Difficulty} - {_difficultyModifier}");
        }

        /// <summary>
        /// Loads all configuration resources from their parent directory to populate their respective config pools.
        /// Runs once on _Ready(). Wave configurations are selected from the loaded resources based on the current wave and the resource's wave threshold.
        /// </summary>
        // private void LoadConfigPools() { }

        #endregion

        #region Scaling

        /// <summary>
        /// Sets the current enemy wave configuration based on the available configurations in the pool.
        /// </summary>
        private void SetScalers()
        {
            _enemyScaleManager.SetCurrentScaler(_wave);
        }

        /// <summary>
        /// Applies the current difficulty modifier to all properties of the current wave configuration.
        /// </summary>
        private void ApplyDifficultyScaling()
        {
            _enemyScaleManager.CurrentEnemyScaler.ApplyDifficultyModifier(_difficultyModifier);
        }

        /// <summary>
        /// Sets each spawner's enemy wave configuration.
        /// </summary>
        private void ScaleSpawners()
        {
            var spawners = GetTree().GetNodesInGroup("enemy-spawners");
            foreach (EnemySpawner spawner in spawners)
            {
                spawner.SetEnemyWaveConfig(_enemyScaleManager.CurrentEnemyScaler);
            }
        }

        #endregion

        #region Wave Play


        private void StartWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            _waveTimer.WaitTime = _waveTime;
            _waveTimer.Start();
            EmitSignal(SignalName.WaveStarted);
        }

        private void EndWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            EmitSignal(SignalName.WaveEnded);
            IncrementWave();
        }

        private void IncrementWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            _wave++;
            ScaleWave();
            // Automatically start the next wave for now.
            // TODO: Don't call this here. Instead, call it at the end of the shop, once the player is ready to move on to the next wave.
            StartWave();
        }

        /// <summary>
        /// Apply all relevant scaling to the current wave.
        /// </summary>
        private void ScaleWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            SetScalers();
            ApplyDifficultyScaling();
            ScaleSpawners();
        }

        public void ResetWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            _wave = 1;
            ScaleWave();
        }

        #endregion
    }
}
