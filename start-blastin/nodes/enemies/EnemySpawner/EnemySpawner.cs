using System;
using System.Linq;
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
        /// Weighted pool of enemies that the spawn point can spawn. The key is the type of enemy, the value is the weighted value of that enemy.
        /// </summary>
        [Export(PropertyHint.ResourceType, "EnemyResource")]
        public Godot.Collections.Dictionary<EnemyResource, int> EnemyPool
        {
            get => _enemyPool;
            set => _enemyPool = value;
        }

        public override void _Ready()
        {
            _path = GetNode<Path2D>("%Path2D");
            _path.Curve = _curve;

            _pathFollow = _path.GetNode<PathFollow2D>("%PathFollow2D");
            _spawnPoint = _pathFollow.GetNode<Node2D>("%SpawnPoint");
            _spawnParent = GetNode<Node>("%SpawnParent");

            _spawnTimer = GetNode<Timer>("%SpawnTimer");
            _spawnTimer.WaitTime = _spawnInterval;
            _spawnTimer.Timeout += SpawnEnemy;
        }

        private EnemyResource GetEnemyFromPool()
        {
            if (_enemyPool == null || _enemyPool.Count == 0)
                return null;

            // Calculate total weight
            int totalWeight = 0;
            foreach (int num in _enemyPool.Values)
            {
                totalWeight += num;
            }

            // Generate random number within total weight
            int randomValue = GD.RandRange(0, totalWeight - 1);

            // Find the enemy that corresponds to this weight
            int currentWeight = 0;
            foreach (var kvp in _enemyPool)
            {
                currentWeight += kvp.Value;
                if (randomValue < currentWeight)
                {
                    return kvp.Key;
                }
            }

            // Fallback
            return _enemyPool.Keys.First();
        }

        private void SpawnEnemy()
        {
            EnemyResource enemyResource = GetEnemyFromPool();
            EnemyNode enemy = EnemyFactory.CreateEnemy(enemyResource);
            enemy.GlobalPosition = _spawnPoint.GlobalPosition;
            _spawnParent.AddChild(enemy);
        }
    }
}
