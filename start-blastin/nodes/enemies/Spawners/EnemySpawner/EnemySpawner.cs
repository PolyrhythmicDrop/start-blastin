using Autoloads;
using Events;
using Factories;
using Godot;
using Utility;
using WaveManagement;

namespace Enemies.Spawners
{
    [GlobalClass]
    public partial class EnemySpawner : Node2D
    {
        private Path2D _path;
        private PathFollow2D _pathFollow;
        private Curve2D _curve;
        private float _pointMoveDuration = 4.0f;
        private float _spawnInterval = 1.6f;
        private SpawnerLocation _location;

        // Base stats
        private float _baseMoveDuration = 4.0f;
        private float _baseSpawnInterval = 1.6f;

        private Timer _spawnTimer;

        private SpawnPool _spawnPool = new();

        // Position-less Node. Add enemies as the child of this node so that their position is not relative to the spawner.
        private Node _spawnParent;

        // Point where the enemies spawn from. Should be the child of _path.
        private Node2D _spawnPoint;

        private EnemyScaler _enemyScaler;

        private int _currentWave;

        /// <summary>
        /// The path this spawner follows.
        /// </summary>
        [Export]
        public Curve2D Curve
        {
            get => _curve;
            set => _curve = value;
        }

        public bool SpawnImmediately = false;

        public float InitialProgressRatio = 0;

        /// <summary>
        /// Percent of the wave that must elapse before the SpawnTimer starts.
        /// </summary>
        public double? SpawnTimeOffset = null;

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

        /// <summary>
        /// Weighted pool of enemies that the spawn point can spawn. The key is the type of enemy, the value is the weighted value of that enemy.
        /// </summary>
        public SpawnPool SpawnPool => _spawnPool;

        /// <summary>
        /// The location of the spawner. Used to affect the spawner's path and spawned enemy paths.
        /// </summary>
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        public override void _Ready()
        {
            _path = GetNode<Path2D>("%Path2D");
            _pathFollow = _path?.GetNode<PathFollow2D>("%PathFollow2D");
            _path.Curve = _curve;

            _spawnPoint = _pathFollow.GetNode<Node2D>("%SpawnPoint");
            _spawnParent = GetNode<Node>("%SpawnParent");

            _spawnTimer = GetNode<Timer>("%SpawnTimer");
            _spawnTimer.WaitTime = _spawnInterval;
            _spawnTimer.Timeout += SpawnEnemy;

            ConnectSignals();

            if (InitialProgressRatio > 0)
            {
                TweenInitialProgress();
            }
            else
            {
                MoveSpawnPoint();
            }
        }

        private void ConnectSignals()
        {
            EventBus.Instance.WaveStarted += OnWaveStarted;
            EventBus.Instance.WaveTimerEnded += OnWaveTimerEnded;
        }

        private void DisconnectSignals()
        {
            EventBus.Instance.WaveStarted -= OnWaveStarted;
            EventBus.Instance.WaveTimerEnded -= OnWaveTimerEnded;
        }

        /// <summary>
        /// Retrieves an enemy resource from the current <see cref="SpawnPool"/>.
        /// </summary>
        private EnemyResource GetEnemyFromPool()
        {
            if (_spawnPool == null || _spawnPool.Count == 0)
                return null;

            // Calculate total weight
            int totalWeight = 0;
            foreach (SpawnData data in _spawnPool)
            {
                totalWeight += data.Weight;
            }

            // Generate random number within total weight
            int randomValue = RNG.GetRandomInt(0, totalWeight - 1);

            // Find the enemy that corresponds to this weight
            int currentWeight = 0;
            foreach (SpawnData data in _spawnPool)
            {
                currentWeight += data.Weight;
                if (randomValue < currentWeight)
                {
                    return data.EnemyResource;
                }
            }
            return _spawnPool[0].EnemyResource;
        }

        /// <summary>
        /// Spawns an enemy from an enemy resource in the spawner's enemy pool.
        /// Applies wave scaling to the spawned enemy.
        /// Creates and sets the path for the enemy using the enemy resource's <see cref="EnemyResource.PathCurve"/> and the spawner's <see cref="_location"/> variable.
        /// Adds the enemy and the new <see cref="EntityPath"/> to the scene tree.
        /// </summary>
        private void SpawnEnemy()
        {
            // Duplicate the enemy resource and create a new EnemyNode based on it.
            EnemyResource enemyResource = (EnemyResource)
                GetEnemyFromPool().DuplicateDeep(Resource.DeepDuplicateMode.Internal);

            // Create an enemy from the factory and apply the current wave scaling.
            EnemyNode enemy = EnemyFactory.CreateEnemy(enemyResource);
            enemy.ApplyWaveScaling(_enemyScaler, _currentWave);

            // Create a new path scene for the new EnemyNode to follow.
            EntityPath entityPath = GD.Load<PackedScene>(EntityPath.ScenePath)
                .Instantiate<EntityPath>();
            entityPath.Curve = enemyResource.PathCurve;
            entityPath.GlobalPosition = _spawnPoint.GlobalPosition;

            switch (_location)
            {
                default:
                case SpawnerLocation.Top:
                    entityPath.RotationDegrees = 0;
                    break;
                case SpawnerLocation.Left:
                    entityPath.RotationDegrees = 270;
                    break;
                case SpawnerLocation.Right:
                    entityPath.RotationDegrees = 90;
                    break;
                case SpawnerLocation.Bottom:
                    entityPath.RotationDegrees = 180;
                    break;
            }

            enemy.SetPath(entityPath);

            _spawnParent.AddChild(entityPath);

            entityPath.PathFollow.AddChild(enemy);

            // Free the path after its associated enemy has left the tree/been despawned.
            enemy.TreeExited += entityPath.QueueFree;

            // Add the enemy to the enemy finder list
            EnemyFinder.AddEnemy(enemy);
        }

        /// <summary>
        /// Moves the spawner's spawn point along its appointed path.
        /// </summary>
        private void MoveSpawnPoint()
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
            initTween.TweenCallback(Callable.From(MoveSpawnPoint));
        }

        public void SetEnemyScaler(EnemyScaler scaler)
        {
            _enemyScaler = scaler;
        }

        /// <summary>
        /// Applies scaling to this EnemySpawner's stats using a passed <see cref="SpawnerScaler"/> object and the current wave number.
        /// </summary>
        /// <param name="spawnerScaler">The scaler to use to scale this spawner's properties.</param>
        /// <param name="wave">The current wave. Used to adjust the wave multiplier of the scaling.</param>
        public void ApplySpawnerScaling(
            int wave,
            SpawnPool spawnPool,
            float spawnIntervalMod,
            float moveDurationMod
        )
        {
            float waveMultiplier = Mathf.Log(1 + wave);
            _spawnPool = spawnPool;

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
        /// Toggles spawn behavior on and off.
        /// </summary>
        /// <param name="spawn">Whether or not to spawn enemies.</param>
        public void ToggleSpawning(bool spawn)
        {
            if (spawn)
            {
                // Spawn an enemy immediately if that feature is enabled
                if (SpawnImmediately)
                {
                    SpawnEnemy();
                }

                // Create a timer to offset spawning if that feature is enabled
                if (SpawnTimeOffset != null)
                {
                    SceneTreeTimer timer = GetTree()
                        .CreateTimer((double)SpawnTimeOffset, processAlways: false);
                    timer.Timeout += () =>
                    {
                        _spawnTimer.Start(_spawnInterval);
                    };
                }
                else
                {
                    _spawnTimer.Start(_spawnInterval);
                }
            }
            else
            {
                _spawnTimer.Stop();
            }
        }

        private void OnWaveStarted(object sender, WaveStartedEventArgs args)
        {
            ToggleSpawning(true);
            _currentWave = args.Wave;
        }

        private void OnWaveTimerEnded()
        {
            ToggleSpawning(false);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
