using System.Reflection;
using System.Threading.Tasks;
using Autoloads;
using Godot;
using Utility;

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
        private SpawnerScaleManager _spawnerScaleManager;

        public int Wave => _wave;

        /// <summary>
        /// Player-selected difficulty. Adjusts the difficulty modifier, which affects per-wave stat scaling.
        /// </summary>
        [Export]
        public Difficulty Difficulty { get; set; } = Difficulty.Easy;

        /// <summary>
        /// Wait time for the <see cref="_waveTimer"/>.
        /// After the wave timer expires, no more enemies are spawned.
        /// </summary>
        [Export]
        public double WaveTime
        {
            get => _waveTime;
            set => _waveTime = value;
        }

        #region Initialization

        /// <summary>
        /// Connects to relevant signals, sets the base difficulty modifier for the game, and initializes the first wave.
        /// </summary>
        public override void _Ready()
        {
            _waveTimer = GetNode<Timer>("%WaveTimer");
            _waveTimer.Timeout += EndWave;
            _waveTimer.WaitTime = _waveTime;

            _enemyScaleManager = GetNode<EnemyScaleManager>("%EnemyScaleManager");
            _spawnerScaleManager = GetNode<SpawnerScaleManager>("%SpawnerScaleManager");

            ConnectSignals();
            SetBaseDifficultyModifier();
            InitializeFirstWave();
        }

        /// <summary>
        /// Sets the base difficulty modifier based on the player's selected difficulty level.
        /// Difficulty modifier is applied to all enemy and spawner stat scaling.
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
                    _difficultyModifier = 0.3f;
                    break;
                case Difficulty.Hard:
                    _difficultyModifier = 0.5f;
                    break;
            }
        }

        /// <summary>
        /// Connects WaveManager signals.
        /// <list type="unordered">
        /// <item><see cref="EventBus.StartWaveButtonPressed"/> => <see cref="StartWave()"/></item>
        /// </list>
        /// </summary>
        private void ConnectSignals()
        {
            EventBus.Instance.StartWaveButtonPressed += StartWave;
        }

        private void DisconnectSignals()
        {
            EventBus.Instance.StartWaveButtonPressed -= StartWave;
        }

        #endregion

        public override void _Process(double delta)
        {
            if (!_waveTimer.IsStopped())
            {
                // EventBus.Instance.EmitSignal(
                //     EventBus.SignalName.WaveTimeLeft,
                //     [_waveTimer.TimeLeft, _waveTime]
                // );
                EventBus.Instance.RaiseWaveTimeLeft(_waveTimer.TimeLeft, _waveTime);
            }
        }

        #region Scaling

        /// <summary>
        /// Sets the current enemy scaler, the spawner scaler, and the spawner formation for the next wave.
        /// </summary>
        private void SetScalers()
        {
            _enemyScaleManager.SetCurrentScalers(_wave);
            _spawnerScaleManager.SetCurrentScalers(_wave);
        }

        /// <summary>
        /// Sets each spawner's enemy wave configuration.
        /// </summary>
        private void ScaleSpawners() =>
            _spawnerScaleManager.ScaleSpawners(
                _enemyScaleManager.CurrentEnemyScaler,
                _difficultyModifier
            );

        #endregion

        #region Wave Play

        /// <summary>
        /// Initializes both scale managers to their default scalers and assembles the default spawner formation.
        /// Scales all spawners according to the default scaler.
        /// </summary>
        private async void InitializeFirstWave()
        {
            GD.Print("Initializing first wave...");
            _enemyScaleManager.Initialize(this);
            _spawnerScaleManager.Initialize(this);
            await _spawnerScaleManager.AssembleFormation();
            GD.Print(
                $"{MethodBase.GetCurrentMethod().Name}: Formation assembled, scaling spawners..."
            );
            ScaleSpawners();

            StartWave();
        }

        /// <summary>
        /// Starts a wave.
        /// Starts the wave timer and emits the <see cref="EventBus.SignalName.WaveStarted"/> signal.
        /// </summary>
        private void StartWave()
        {
            GD.Print($"Wave {_wave} starting!");
            _waveTimer.Start(_waveTime);
            // EventBus.Instance.EmitSignal(EventBus.SignalName.WaveStarted, _wave);
            EventBus.Instance.RaiseWaveStarted(_wave);
        }

        /// <summary>
        /// Ends a wave.
        /// Emits the <see cref="EventBus.WaveTimerEnded"/> event, then waits for all enemies to be freed before raising the WaveComplete event and incrementing the next wave.
        /// </summary>
        private async void EndWave()
        {
            // Emit the signal for the wave timer ending to stop enemy spawning
            GD.Print($"Wave {_wave} timer ended!");
            // EventBus.Instance.EmitSignal(EventBus.SignalName.WaveTimerEnded);
            EventBus.Instance.RaiseWaveTimerEnded();

            // Wait for all enemies to clear before processing the next wave.
            bool enemiesCleared = await WaitForEnemiesToClear();
            GD.Print($"Enemies cleared: {enemiesCleared}");
            EventBus.Instance.RaiseWaveComplete();

            IncrementWave();
        }

        /// <summary>
        /// Debug version of ending a wave. Used by the <see cref="Debugger"/> class to manually end a wave.
        /// </summary>
        public void DebugEndWave()
        {
            if (!_waveTimer.IsStopped())
            {
                _waveTimer.Stop();
            }
            EndWave();
        }

        /// <summary>
        /// Asynchronous method that advances frames until no enemies remain in the tree.
        /// Returns true when no enemies remain.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> WaitForEnemiesToClear()
        {
            int enemyCount = GetTree().GetNodesInGroup("enemies").Count;
            int prevEnemyCount = enemyCount;
            while ((enemyCount = GetTree().GetNodesInGroup("enemies").Count) > 0)
            {
                if (enemyCount != prevEnemyCount)
                {
                    GD.Print($"Enemy count: {enemyCount}");
                    prevEnemyCount = enemyCount;
                }
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            return true;
        }

        /// <summary>
        /// Increments the current wave number and calls <see cref="ScaleWave()"/> to set the next wave's scalers.
        /// </summary>
        private void IncrementWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            _wave++;
            ScaleWave();
        }

        /// <summary>
        /// Sets the scalers to use for the next wave,
        /// assembles the spawner formation, and scales all spawners according to the selected scalers.
        /// </summary>
        private async void ScaleWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            SetScalers();
            await _spawnerScaleManager.AssembleFormation();
            ScaleSpawners();
        }

        /// <summary>
        /// Resets the wave to the first wave and reinitializes all scalers and scale managers.
        /// </summary>
        public void ResetWave()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}");
            _wave = 1;
            InitializeFirstWave();
        }

        #endregion

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
