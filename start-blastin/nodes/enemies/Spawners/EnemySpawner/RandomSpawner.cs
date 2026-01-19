using System.Collections.Generic;
using System.Linq;
using Autoloads;
using Components;
using Events;
using Factories;
using Godot;
using Interfaces;
using Utility;

namespace Enemies.Spawners
{
    [GlobalClass]
    public partial class RandomSpawner : EnemySpawner, IListener
    {
        private float _pointMoveDuration = 4.0f;
        private float _spawnInterval = 1.6f;

        // Base stats
        private float _baseMoveDuration = 4.0f;
        private float _baseSpawnInterval = 1.6f;

        private Timer _spawnTimer;

        public bool SpawnImmediately = false;

        public bool StartMoveOnSpawnTimer = false;

        public float InitialProgressRatio = 0;

        /// <summary>
        /// Percent of the wave that must elapse before the SpawnTimer starts.
        /// </summary>
        public double SpawnTimerDelay = 0;

        /// <summary>
        /// The seconds that elapse before a new enemy is spawned from the spawner.
        /// </summary>
        public float SpawnInterval => _spawnInterval;

        /// <summary>
        /// The time it takes for the spawn point to move to the end of the Path2D curve.
        /// Higher values result in a slower-moving spawn point.
        /// </summary>
        /// <remarks>
        /// Used to set the duration parameter of the spawn point move Tween.
        /// </remarks>
        public float SpawnPointMoveDuration => _pointMoveDuration;

        private Dictionary<SpawnData, int> _spawnPool;
        public Dictionary<SpawnData, int> SpawnPool
        {
            get => _spawnPool;
            set => _spawnPool = value;
        }

        public override void _Ready()
        {
            base._Ready();

            _spawnTimer = GetNode<Timer>("%SpawnTimer");
            _spawnTimer.WaitTime = _spawnInterval;
            _spawnTimer.Timeout += SpawnEnemy;

            ConnectSignals();

            if (!StartMoveOnSpawnTimer)
            {
                MoveSpawnPoint();
            }
        }

        protected override void OnWaveStarted(object sender, WaveStartedEventArgs args)
        {
            _waveTimerActive = true;
            _currentWave = args.Wave;
            ToggleSpawning(true);
        }

        protected override void OnWaveTimerEnded()
        {
            _waveTimerActive = false;
            ToggleSpawning(false);
        }

        /// <summary>
        /// Begins moving the spawn point.
        /// </summary>
        private void MoveSpawnPoint()
        {
            if (InitialProgressRatio > 0)
            {
                TweenInitialProgress();
            }
            else
            {
                StartMoveLoop();
            }
        }

        /// <summary>
        /// Handles initial spawner movement if you set an initial progress ratio other than 0.
        /// </summary>
        private void TweenInitialProgress()
        {
            // Set the initial move-to point if we start from a different spot than origin.
            _pathFollow.ProgressRatio = InitialProgressRatio;
            float initialDuration =
                _pointMoveDuration - (_pointMoveDuration * InitialProgressRatio);

            Tween initTween = _pathFollow.CreateTween();
            // Go to the end of the curve using the new duration.
            initTween.TweenProperty(_pathFollow, "progress_ratio", 1.0, initialDuration);
            // Go back to the beginning of the curve to reset the loop.
            initTween
                .TweenProperty(_pathFollow, "progress_ratio", 0, _pointMoveDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            // Call the normal MoveSpawnPoint() method to begin normal looping.
            initTween.TweenCallback(Callable.From(StartMoveLoop));
        }

        /// <summary>
        /// Moves the spawner's spawn point back and forth along its appointed path in an eternal loop.
        /// </summary>
        private void StartMoveLoop()
        {
            Tween tween = CreateTween();
            tween
                .TweenProperty(_pathFollow, "progress_ratio", 1.0, _pointMoveDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.OutIn);
            tween
                .TweenProperty(_pathFollow, "progress_ratio", 0, _pointMoveDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.SetLoops();
        }

        /// <summary>
        /// Retrieves an enemy resource from the current <see cref="SpawnPool"/>.
        /// </summary>
        private SpawnData GetSpawnDataFromPool()
        {
            if (_spawnPool == null || _spawnPool.Count == 0)
            {
                DebugLogger.LogMessage($"_spawnPool isn't populated!", true, true);
                return null;
            }

            // Calculate total weight using the values of the spawn pool.
            int totalWeight = 0;
            foreach (int weight in _spawnPool.Values)
            {
                totalWeight += weight;
            }

            // Generate random number within total weight
            int randomValue = RNG.GetRandomInt(0, totalWeight - 1);

            // Find the spawn data that corresponds to this weight
            int currentWeight = 0;
            foreach (KeyValuePair<SpawnData, int> kvp in _spawnPool)
            {
                currentWeight += kvp.Value;
                if (randomValue < currentWeight)
                {
                    return kvp.Key;
                }
            }
            // If you can't find an enemy to return, simply return the first enemy in the pool.
            return _spawnPool.First().Key;
        }

        /// <summary>
        /// Applies scaling to this EnemySpawner's stats using a passed <see cref="SpawnerScaler"/> object and the current wave number.
        /// </summary>
        /// <param name="spawnerScaler">The scaler to use to scale this spawner's properties.</param>
        /// <param name="wave">The current wave. Used to adjust the wave multiplier of the scaling.</param>
        public override void ApplySpawnerScaling(
            int wave,
            float spawnIntervalMod,
            float moveDurationMod
        )
        {
            float waveMultiplier = Mathf.Log(1 + wave);
            // _spawnPool = spawnPool;

            // Don't apply scaling on the first wave.
            if (wave == 1)
            {
                _spawnInterval = _baseSpawnInterval;
                _pointMoveDuration = _baseMoveDuration;
                return;
            }
            // Percentage application
            float spawnPercentReduction = (spawnIntervalMod / 100f) * waveMultiplier;
            _spawnInterval = Mathf.Max(0.1f, _baseSpawnInterval * (1 - spawnPercentReduction));

            float movePercentReduction = (moveDurationMod / 100f) * waveMultiplier;
            _pointMoveDuration = Mathf.Max(0.2f, _baseMoveDuration * (1 - movePercentReduction));
        }

        /// <summary>
        /// Pulls a semi-random set of <see cref="SpawnData"/> from the spawn pool and passes it to the base <see cref="EnemySpawner.SpawnEnemy(SpawnData)"/> method.
        /// </summary>
        protected void SpawnEnemy()
        {
            if (!_waveTimerActive)
            {
                return;
            }

            // Get spawn data from the pool.
            base.SpawnEnemy(GetSpawnDataFromPool());
        }

        /// <summary>
        /// Toggles spawn behavior on and off.
        /// </summary>
        /// <param name="spawn">Whether or not to spawn enemies.</param>
        public override void ToggleSpawning(bool spawn)
        {
            if (spawn)
            {
                // Create a timer to offset spawning if that feature is enabled
                if (SpawnTimerDelay != 0)
                {
                    SceneTreeTimer timer = GetTree()
                        .CreateTimer(SpawnTimerDelay, processAlways: false);
                    timer.Timeout += StartSpawnTimer;
                }
                else
                {
                    StartSpawnTimer();
                }
            }
            else
            {
                _spawnTimer.Stop();
            }
        }

        protected override void StartSpawnTimer()
        {
            if (!_waveTimerActive)
            {
                return;
            }
            // Spawn an enemy immediately if that feature is enabled
            if (SpawnImmediately)
            {
                SpawnEnemy();
            }

            if (StartMoveOnSpawnTimer)
            {
                MoveSpawnPoint();
            }

            _spawnTimer.Start(_spawnInterval);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
