using Autoloads;
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
        /// <summary>
        /// Cached scene for the EntityPath node that spawned enemies will follow.
        /// </summary>
        private PackedScene _pathScene = GD.Load<PackedScene>("uid://da7sqkjhcrp8a");

        /// <summary>
        /// The path of the spawner.
        /// </summary>
        protected Path2D _path;

        /// <summary>
        /// The PathFollow2D node the spawner is a child of.
        /// </summary>
        protected PathFollow2D _pathFollow;

        /// <summary>
        /// The curve of the <see cref="_path"/> that the spawner follows.
        /// </summary>
        protected Curve2D _curve;

        /// <summary>
        /// The location of the EnemySpawner, used to set the rotation of enemies it spawns.
        /// </summary>
        protected SpawnerLocation _location;

        /// <summary>
        /// Whether or not a wave is currently running.
        /// </summary>
        protected bool _waveTimerActive = false;

        /// <summary>
        /// Position-less Node. Add enemies as the child of this node so that their position is not relative to the spawner.
        /// </summary>
        protected Node _spawnParent;

        /// <summary>
        /// Point where the enemies spawn from. Should be the child of _path.
        /// </summary>
        protected Node2D _spawnPoint;

        /// <summary>
        /// The Scaler resource used to scale enemies spawned from this spawner.
        /// </summary>
        protected EnemyScaler _enemyScaler;

        /// <summary>
        /// The current wave number, used to scale enemies and the spawner appropriately.
        /// </summary>
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

        public void ConnectSignals()
        {
            EventBus.Instance.WaveStarted += OnWaveStarted;
            EventBus.Instance.WaveTimerEnded += OnWaveTimerEnded;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.WaveStarted -= OnWaveStarted;
            EventBus.Instance.WaveTimerEnded -= OnWaveTimerEnded;
        }

        /// <summary>
        /// Spawns an enemy (or enemies, if <see cref="SpawnData.SquadEnabled"/> is true) from spawn data.
        /// Applies wave scaling to the spawned enemy.
        /// Creates and sets the path for the enemy using the enemy resource's <see cref="EnemyResource.PathCurve"/> and the spawner's <see cref="_location"/> variable.
        /// Adds the enemy and the new <see cref="EntityPath"/> to the scene tree and to the <see cref="EnemyFinder"/> collection.
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

        /// <summary>
        /// Sets the current enemy scaler for scaling enemies.
        /// </summary>
        /// <param name="scaler"></param>
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
