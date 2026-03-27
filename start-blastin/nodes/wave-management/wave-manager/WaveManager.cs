using System.Linq;
using System.Threading.Tasks;
using Autoloads;
using Godot;
using UI;
using Utility;

namespace WaveManagement
{
    [GlobalClass]
    public partial class WaveManager : Node
    {
        private PackedScene _countdownScene = GD.Load<PackedScene>("uid://cj20n8pswpaby");
        private PackedScene _completeScene = GD.Load<PackedScene>("uid://dupt8gxk8crm2");
        private bool _overrideNextWaveNumber = false;
        private int _nextWaveNumber;

        private int _wave = 1;
        private float _difficultyModifier = 0.1f;
        private Timer _waveTimer;
        private static double _waveTime = 30;

        private ScaleManager _scaleManager;

        public int Wave => _wave;

        public ScaleManager ScaleManager => _scaleManager;

        public string LevelOneScaler
        {
            get => _scaleManager?.LevelOneFormation;
            set => _scaleManager?.LevelOneFormation = value;
        }

        /// <summary>
        /// Player-selected difficulty. Adjusts the difficulty modifier, which affects per-wave stat scaling.
        /// </summary>
        public Difficulty Difficulty { get; set; } = Difficulty.Easy;

        public float DifficultyModifier => _difficultyModifier;

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
            _waveTimer.WaitTime = _waveTime;
            _waveTimer.Timeout += EndWave;

            _scaleManager = GetNode<ScaleManager>("%ScaleManager");

            ConnectSignals();
            SetBaseDifficultyModifier();
        }

        /// <summary>
        /// Sets the base difficulty modifier based on the player's selected difficulty level.
        /// Difficulty modifier is applied to all enemy and spawner stat scaling.
        /// </summary>
        private void SetBaseDifficultyModifier()
        {
            _difficultyModifier = Difficulty switch
            {
                Difficulty.Easy => 0.1f,
                Difficulty.Medium => 0.3f,
                Difficulty.Hard => 0.5f,
                _ => 0.3f,
            };
        }

        private void ConnectSignals()
        {
            EventBus.Instance.StartWaveButtonPressed += StartWave;
            EventBus.Instance.GameOver += PauseWaveTimer;
        }

        private void DisconnectSignals()
        {
            EventBus.Instance.StartWaveButtonPressed -= StartWave;
            EventBus.Instance.GameOver -= PauseWaveTimer;
        }

        #endregion

        public override void _Process(double delta)
        {
            if (!_waveTimer.IsStopped())
            {
                EventBus.Instance.RaiseWaveTimeLeft(_waveTimer.TimeLeft, _waveTime);
            }
        }

        #region Scaling

        /// <summary>
        /// Sets the current enemy scaler, the spawner scaler, and the spawner formation for the next wave.
        /// </summary>
        private void SetScalers()
        {
            _scaleManager.SetCurrentScalers(_wave);
        }

        #endregion

        #region Wave Play

        /// <summary>
        /// Initializes both scale managers to their default scalers and assembles the default spawner formation.
        /// Scales all spawners according to the default scaler.
        /// </summary>
        public async void InitializeFirstWave()
        {
            _scaleManager.Initialize(this);
            await _scaleManager.AssembleFormation();

            StartWave();
        }

        /// <summary>
        /// Starts a wave.
        /// Starts the wave timer and emits the <see cref="EventBus.SignalName.WaveStarted"/> signal.
        /// </summary>
        private async void StartWave()
        {
            await CountDownWave();
            _waveTimer.Start(_waveTime);
            EventBus.Instance.RaiseWaveStarted(_wave);
        }

        private void PauseWaveTimer()
        {
            _waveTimer.Paused = true;
        }

        /// <summary>
        /// Creates a <see cref="WaveCountdown"/> object and plays the pre-wave countdown.
        /// </summary>
        private async Task CountDownWave()
        {
            WaveCountdown wc = _countdownScene.Instantiate<WaveCountdown>();
            AddChild(wc);

            await wc.Start();

            wc.QueueFree();
        }

        /// <summary>
        /// Ends a wave.
        /// Emits the <see cref="EventBus.WaveTimerEnded"/> event, then waits for all enemies to be freed before raising the WaveComplete event and incrementing the next wave.
        /// </summary>
        private async void EndWave()
        {
            EventBus.Instance.RaiseWaveTimerEnded();

            // Wait for all enemies to clear before processing the next wave.
            await WaitForEnemiesToClear();

            IncrementWave();

            await PlayWaveCompleteAnim();

            EventBus.Instance.RaiseWaveComplete();
        }

        private async Task PlayWaveCompleteAnim()
        {
            // Show the wave complete thingie.
            WaveComplete wc = _completeScene.Instantiate<WaveComplete>();
            AddChild(wc);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await wc.PlayWaveCompleteAnimation();
            if (!wc.IsQueuedForDeletion())
            {
                wc.QueueFree();
            }
            await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
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

        public void DebugOverrideNextWaveNumber(int waveNumber)
        {
            _overrideNextWaveNumber = true;
            _nextWaveNumber = waveNumber;
        }

        /// <summary>
        /// Asynchronous method that advances frames until no enemies remain in the tree.
        /// Returns true when no enemies remain.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> WaitForEnemiesToClear()
        {
            // int enemyCount = EnemyFinder.GetAllEnemies().Count();
            // int prevEnemyCount = enemyCount;
            // while ((enemyCount = EnemyFinder.GetAllEnemies().Count()) > 0)
            int enemyCount = EnemyFinder.GetOnScreenEnemies(true).Count();
            int prevEnemyCount = enemyCount;
            while ((enemyCount = EnemyFinder.GetOnScreenEnemies(true).Count()) > 0)
            {
                if (enemyCount != prevEnemyCount)
                {
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
            if (_overrideNextWaveNumber)
            {
                _wave = _nextWaveNumber;
                _overrideNextWaveNumber = false;
            }
            else
            {
                _wave++;
            }
            ScaleWave();
        }

        /// <summary>
        /// Sets the scalers to use for the next wave,
        /// assembles the spawner formation, and scales all spawners according to the selected scalers.
        /// </summary>
        private async void ScaleWave()
        {
            SetScalers();
            await _scaleManager.AssembleFormation();
        }

        /// <summary>
        /// Resets the wave to the first wave and reinitializes all scalers and scale managers.
        /// </summary>
        public void ResetWave()
        {
            _wave = 1;
            InitializeFirstWave();
        }

        /// <summary>
        /// Stops the wave timer, then restarts the current wave from the beginning. Does not apply new scaling or select a new formation.
        /// </summary>
        public void RestartCurrentWave()
        {
            _waveTimer.Stop();
            StartWave();
        }

        #endregion

        public static double GetNextWaveTime()
        {
            return _waveTime;
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
