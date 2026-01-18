using Components;
using Events;
using Factories;
using Godot;
using Utility;
using WaveManagement;

namespace Enemies.Spawners
{
    /// <summary>
    /// Base class for concrete EnemySpawner objects. Contains the basic elements for spawning enemies from outside of the play area.
    /// </summary>
    [GlobalClass]
    public partial class EnemySpawner : Node2D
    {
        private PackedScene _pathScene = GD.Load<PackedScene>("uid://da7sqkjhcrp8a");
        protected Path2D _path;
        protected PathFollow2D _pathFollow;
        protected Curve2D _curve;
        protected SpawnerLocation _location;

        protected bool _waveTimerActive = false;

        /// <summary>
        /// Position-less Node. Add enemies as the child of this node so that their position is not relative to the spawner.
        /// </summary>
        protected Node _spawnParent;

        /// <summary>
        /// Point where the enemies spawn from. Should be the child of _path.
        /// </summary>
        protected Node2D _spawnPoint;

        protected EnemyScaler _enemyScaler;

        protected int _currentWave;

        /// <summary>
        /// The path this spawner follows.
        /// </summary>
        [Export]
        public Curve2D Curve
        {
            get => _curve;
            set => _curve = value;
        }

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
        }

        /// <summary>
        /// Spawns an enemy from spawn data.
        /// Applies wave scaling to the spawned enemy.
        /// Creates and sets the path for the enemy using the enemy resource's <see cref="EnemyResource.PathCurve"/> and the spawner's <see cref="_location"/> variable.
        /// Adds the enemy and the new <see cref="EntityPath"/> to the scene tree.
        /// </summary>
        protected virtual void SpawnEnemy(SpawnData spawnData)
        {
            EnemyResource enemyResource = (EnemyResource)
                ResourceLoader.Load<EnemyResource>(spawnData.EnemyType).Duplicate(true);

            // Determine number of enemies to spawn based on whether we're in a squadron or not.
            int condition = spawnData.SquadEnabled ? spawnData.Squadron.Offsets.Count : 1;

            for (int i = 0; i < condition; i++)
            {
                // Create an enemy from the factory and apply the current wave scaling.
                EnemyNode enemy = EnemyFactory.CreateEnemy(enemyResource);
                enemy.ApplyWaveScaling(_enemyScaler, _currentWave);

                if (spawnData.SquadEnabled)
                {
                    enemy.InSquadron = true;
                    enemy.SplitPoint = spawnData.SplitPoint;
                    enemy.SquadronPosition = spawnData.Squadron.Offsets[i];
                }
                // Create a new path scene for the new EnemyNode to follow.
                EntityPath entityPath = _pathScene.Instantiate<EntityPath>();
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
        public virtual void ApplySpawnerScaling(
            int wave,
            float spawnIntervalMod,
            float moveDurationMod
        ) { }

        /// <summary>
        /// Toggles spawn behavior on and off.
        /// </summary>
        /// <param name="spawn">Whether or not to spawn enemies.</param>
        public virtual void ToggleSpawning(bool spawn) { }

        protected virtual void StartSpawnTimer() { }

        protected virtual void OnWaveStarted(object sender, WaveStartedEventArgs args) { }

        protected virtual void OnWaveTimerEnded() { }
    }
}
