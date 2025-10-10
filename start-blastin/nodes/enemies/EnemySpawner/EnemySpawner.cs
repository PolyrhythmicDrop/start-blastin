using System.Linq;
using System.Reflection;
using Factories;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class EnemySpawner : Node2D
    {
        private Path2D _path;
        private PathFollow2D _pathFollow;
        private Curve2D _curve;
        private float _pointMoveDuration;
        private float _spawnInterval;
        private Timer _spawnTimer;
        private Godot.Collections.Dictionary<EnemyResource, int> _enemyPool;

        // Position-less Node. Add enemies as the child of this node so that their position is not relative to the spawner.
        private Node _spawnParent;

        // Point where the enemies spawn from. Should be the child of _path.
        private Node2D _spawnPoint;

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
        public Godot.Collections.Dictionary<EnemyResource, int> EnemyPool
        {
            get => _enemyPool;
            set => _enemyPool = value;
        }

        public override void _Ready()
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            );
            _path = GetNode<Path2D>("%Path2D");
            // _path.Curve = _curve;

            _pathFollow = _path.GetNode<PathFollow2D>("%PathFollow2D");
            _spawnPoint = _pathFollow.GetNode<Node2D>("%SpawnPoint");
            _spawnParent = GetNode<Node>("%SpawnParent");

            _spawnTimer = GetNode<Timer>("%SpawnTimer");
            _spawnTimer.WaitTime = _spawnInterval;
            _spawnTimer.Timeout += SpawnEnemy;
            _spawnTimer.Start();

            MoveSpawnPoint();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
        }

        private EnemyResource GetEnemyFromPool()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name} -> \n");
            if (_enemyPool == null || _enemyPool.Count == 0)
                return null;

            // Calculate total weight
            int totalWeight = 0;
            foreach (int num in _enemyPool.Values)
            {
                totalWeight += num;
            }

            GD.Print($"Total weight: {totalWeight}");

            // Generate random number within total weight
            int randomValue = GD.RandRange(0, totalWeight - 1);

            GD.Print($"Random value: {randomValue}");

            // Find the enemy that corresponds to this weight
            int currentWeight = 0;
            foreach (var kvp in _enemyPool)
            {
                currentWeight += kvp.Value;
                if (randomValue < currentWeight)
                {
                    GD.Print($"Returning {kvp.Key} from foreach loop!");
                    return kvp.Key;
                }
            }

            // Fallback
            GD.Print(
                $"Something went wrong! Returning first key in enemy pool, which is: {_enemyPool.Keys.First()}"
            );
            return _enemyPool.Keys.First();
        }

        private void SpawnEnemy()
        {
            GD.Print($"{MethodBase.GetCurrentMethod().Name}: Attempting to spawn enemy...");
            EnemyResource enemyResource = GetEnemyFromPool();
            EnemyNode enemy = EnemyFactory.CreateEnemy(enemyResource);

            GD.Print($"{MethodBase.GetCurrentMethod().Name}: {enemy.Name} created from factory!");
            enemy.GlobalPosition = _spawnPoint.GlobalPosition;
            GD.Print(
                $"{MethodBase.GetCurrentMethod().Name}: {enemy} global position set to {enemy.GlobalPosition}."
            );
            _spawnParent.AddChild(enemy);
            GD.Print(
                $"{MethodBase.GetCurrentMethod().Name}: {enemy} added to tree! Enemy global position: {enemy.GlobalPosition}."
            );
        }

        private void MoveSpawnPoint()
        {
            Tween tween = CreateTween();
            tween.TweenProperty(_pathFollow, "progress_ratio", 1.0, _pointMoveDuration);
            tween.TweenProperty(_pathFollow, "progress_ratio", 0, _pointMoveDuration);
            tween.SetLoops();
        }
    }
}
