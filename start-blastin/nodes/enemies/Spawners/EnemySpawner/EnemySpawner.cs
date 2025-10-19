using System;
using System.Reflection;
using Autoloads;
using Factories;
using Godot;
using WaveManagement;

namespace Enemies.Spawners
{
    [GlobalClass]
    public partial class EnemySpawner : Node2D
    {
        private Path2D _path;
        private PathFollow2D _pathFollow;
        private Curve2D _curve;
        private float _pointMoveDuration = 5.0f;
        private float _spawnInterval = 2.0f;
        private SpawnerLocation _location;

        // Base stats
        private float _baseMoveDuration = 5.0f;
        private float _baseSpawnInterval = 2.0f;

        private Timer _spawnTimer;

        private SpawnPool _spawnPool = new();

        // Position-less Node. Add enemies as the child of this node so that their position is not relative to the spawner.
        private Node _spawnParent;

        // Point where the enemies spawn from. Should be the child of _path.
        private Node2D _spawnPoint;

        private EnemyScaler _enemyScaler;

        private int _currentWave;

        [Export]
        public Curve2D Curve
        {
            get => _curve;
            set => _curve = value;
        }

        [Export]
        public float SpawnInterval
        {
            get => _spawnInterval;
            set => _spawnInterval = value;
        }

        /// <summary>
        /// The time it takes for the spawn point to move to the end of the Path2D curve.
        /// Higher values result in a slower-moving spawn point.
        /// </summary>
        /// <remarks>
        /// Used to set the duration parameter of the spawn point move Tween.
        /// </remarks>
        [Export]
        public float SpawnPointMoveDuration
        {
            get => _pointMoveDuration;
            set => _pointMoveDuration = value;
        }

        /// <summary>
        /// Weighted pool of enemies that the spawn point can spawn. The key is the type of enemy, the value is the weighted value of that enemy.
        /// </summary>
        [Export]
        public Godot.Collections.Array<SpawnData> SpawnPool
        {
            get => _spawnPool.ConvertToGodotArray();
            set => _spawnPool = new SpawnPool(value);
        }

        [Export]
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        public int CurrentWave
        {
            get => _currentWave;
            set => _currentWave = value;
        }

        public override void _Ready()
        {
            // Set base stats:
            // _baseMoveDuration = _pointMoveDuration;
            // _baseSpawnInterval = _spawnInterval;

            _path = GetNode<Path2D>("%Path2D");
            _pathFollow = _path.GetNode<PathFollow2D>("%PathFollow2D");
            _path.Curve = _curve;

            _spawnPoint = _pathFollow.GetNode<Node2D>("%SpawnPoint");
            _spawnParent = GetNode<Node>("%SpawnParent");

            _spawnTimer = GetNode<Timer>("%SpawnTimer");
            _spawnTimer.WaitTime = _spawnInterval;
            _spawnTimer.Timeout += SpawnEnemy;

            // Connect to wave signals
            EventBus.Instance.Connect(
                EventBus.SignalName.WaveStarted,
                Callable.From(
                    (int wave) =>
                    {
                        ToggleSpawning(true);
                        _currentWave = wave;
                    }
                )
            );

            EventBus.Instance.Connect(
                EventBus.SignalName.WaveEnded,
                Callable.From(() => ToggleSpawning(false))
            );

            // ToggleSpawning(true);
            MoveSpawnPoint();

            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} finished!"
            );
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
        }

        private EnemyResource GetEnemyFromPool()
        {
            // GD.Print($"{MethodBase.GetCurrentMethod().Name}:\n");
            if (_spawnPool == null || _spawnPool.Count == 0)
                return null;

            // Calculate total weight
            int totalWeight = 0;
            foreach (SpawnData data in _spawnPool)
            {
                totalWeight += data.Weight;
            }

            // GD.Print($"Total weight: {totalWeight}");

            // Generate random number within total weight
            int randomValue = GD.RandRange(0, totalWeight - 1);

            // GD.Print($"Random value: {randomValue}");

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

            // Fallback
            GD.PrintErr(
                $"Something went wrong! Returning first EnemyResource in enemy pool, which is: {_spawnPool[0].EnemyResource}"
            );
            return _spawnPool[0].EnemyResource;
        }

        private void SpawnEnemy()
        {
            // Duplicate the enemy resource and create a new EnemyNody based on it.
            EnemyResource enemyResource = (EnemyResource)
                GetEnemyFromPool().DuplicateDeep(Resource.DeepDuplicateMode.Internal);

            // Create an enemy from the factory and apply the current wave configuration.
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
        }

        private void MoveSpawnPoint()
        {
            Tween tween = CreateTween();
            tween.TweenProperty(_pathFollow, "progress_ratio", 1.0, _pointMoveDuration);
            tween.TweenProperty(_pathFollow, "progress_ratio", 0, _pointMoveDuration);
            tween.SetLoops();
        }

        public void SetEnemyScaler(EnemyScaler scaler)
        {
            _enemyScaler = scaler;
        }

        public void ApplySpawnerScaler(SpawnerScaler spawnerScaler, int wave)
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}"
            );
            float waveMultiplier = Mathf.Log(1 + wave);
            _spawnPool = new SpawnPool(spawnerScaler.SpawnPool);

            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Spawn pool applied!"
            );
            foreach (SpawnData spawnData in _spawnPool)
            {
                GD.Print($"{spawnData.EnemyResource.ResourceName} | Weight: {spawnData.Weight}");
            }

            // Don't apply scaling on the first wave.
            if (wave == 1)
            {
                _spawnInterval = _baseSpawnInterval;
                _pointMoveDuration = _baseMoveDuration;
                return;
            }
            // Percentage application
            float spawnPercentReduction =
                (spawnerScaler.SpawnIntervalModifier / 100f) * waveMultiplier;
            _spawnInterval = Mathf.Max(0.1f, _baseSpawnInterval * (1 - spawnPercentReduction));

            float movePercentReduction =
                (spawnerScaler.MoveDurationModifier / 100f) * waveMultiplier;
            _pointMoveDuration = Mathf.Max(0.2f, _baseMoveDuration * (1 - movePercentReduction));

            // float logSpawnModifier = spawnerScaler.SpawnIntervalModifier * waveMultiplier;
            // _spawnInterval = Mathf.Max(0.1f, _baseSpawnInterval * (1 - logSpawnModifier));

            // float logMoveModifier = spawnerScaler.MoveDurationModifier * waveMultiplier;
            // _pointMoveDuration = Mathf.Max(0.2f, _baseMoveDuration * (1 - logMoveModifier));

            GD.Print(
                $"{MethodBase.GetCurrentMethod().Name}: Spawner scaler {spawnerScaler.ResourceName} applied! Interval: {_spawnInterval} | Move Duration {_pointMoveDuration}"
            );
        }

        /// <summary>
        /// Toggles spawn behavior on and off.
        /// </summary>
        /// <param name="spawn">Whether or not to spawn enemies.</param>
        public void ToggleSpawning(bool spawn)
        {
            GD.Print($"{Name}.{MethodBase.GetCurrentMethod().Name}: Toggling spawning to {spawn}!");
            if (spawn)
            {
                _spawnTimer.Start(_spawnInterval);
            }
            else
            {
                _spawnTimer.Stop();
            }
        }
    }
}
