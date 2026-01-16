using Autoloads;
using Godot;
using NanoidDotNet;

namespace Enemies.Spawners
{
    public enum SpawnerLocation
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    /// <summary>
    /// Configuration for an EnemySpawner object, including the spawner's location and <see cref="SpawnPool"/>,
    /// Used by a SpawnerFormationScaler and the ScaleManager to generate spawners.
    /// </summary>
    [GlobalClass]
    public partial class SpawnerConfig : Resource
    {
        private SpawnerLocation _location = SpawnerLocation.Top;

        private SpawnPool _spawnPool;

        private Godot.Collections.Array<SpawnData> _spawnPoolGD;

        private float _minSpawnDelay;
        private float _maxSpawnDelay;

        [Export]
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        /// <summary>
        /// Spawns an enemy as soon as the spawn timer starts.
        /// </summary>
        [Export]
        public bool SpawnImmediately { get; set; } = false;

        /// <summary>
        /// If true, begins moving the spawner only when the spawn timer has started.
        /// If false, the spawner begins moving as soon as it enters the scene tree, regardless of whether it's spawning enemies or not.
        /// </summary>
        [Export]
        public bool StartMoveOnSpawnTimer { get; set; } = false;

        /// <summary>
        /// The progress ratio the EnemySpawner begins at, or the point along its path that it begins.
        /// Represented by a range of 0 to 1.0, where 0 is at the start of the path and 1.0 is at the end of the path.
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float InitialProgressRatio { get; set; } = 0;

        [ExportGroup("Spawn Timer Delay")]
        [Export(PropertyHint.GroupEnable)]
        public bool EnableSpawnTimerDelay { get; set; }

        /// <summary>
        /// Minimum factor to delay the spawn timer by, in a range from 0 (starts the spawn timer immediately on wave start) to 1.0 (starts the spawn timer at the very end of the wave, i.e. never spawns)
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float MinDelay
        {
            get => _minSpawnDelay;
            set => _minSpawnDelay = value;
        }

        /// <summary>
        /// Maximum factor to delay the spawn timer by, in a range from 0 (starts the spawn timer immediately on wave start) to 1.0 (starts the spawn timer at the very end of the wave, i.e. never spawns)
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float MaxDelay
        {
            get => _maxSpawnDelay;
            set => _maxSpawnDelay = value;
        }

        public SpawnPool SpawnPool => _spawnPool;

        [ExportGroup("Spawn Pool")]
        [Export]
        public Godot.Collections.Array<SpawnData> SpawnPoolGD
        {
            get => _spawnPoolGD;
            set
            {
                _spawnPool = [.. value];
                _spawnPoolGD = value;
            }
        }

        public void ConfigureSpawner(EnemySpawner spawner, double? waveTime = null)
        {
            // Set the spawner's position, size, and rotation based on the location.
            Vector2 position;
            float rotationDegrees;
            Curve2D curve;

            switch (_location)
            {
                default:
                case SpawnerLocation.Top:
                    position = new Vector2(50, -82);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
                case SpawnerLocation.Left:
                    position = new Vector2(-82, 50);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Right:
                    position = new Vector2(2000, 1100);
                    rotationDegrees = 180;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Bottom:
                    position = new Vector2(1870, 1162);
                    rotationDegrees = 180;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
            }

            spawner.Name = $"{spawner.GetType().Name}-{Nanoid.Generate(size: 5)}";
            spawner.Curve = curve;
            spawner.Position = position;
            spawner.RotationDegrees = rotationDegrees;
            spawner.Location = _location;
            spawner.SpawnImmediately = SpawnImmediately;
            spawner.StartMoveOnSpawnTimer = StartMoveOnSpawnTimer;
            spawner.InitialProgressRatio = InitialProgressRatio;

            // Set spawn offset
            if (EnableSpawnTimerDelay && waveTime != null)
            {
                // Get the current wave time
                // Get a random value between the min and the max.
                spawner.SpawnTimerDelay =
                    (double)waveTime * RNG.GetRandomDouble(MinDelay, MaxDelay);
            }
        }
    }
}
