using Autoloads;
using Enemies;
using Enemies.Spawners;
using Godot;
using NanoidDotNet;

namespace WaveManagement
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

        private float _minSpawnOffset;
        private float _maxSpawnOffset;

        [Export]
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        /// <summary>
        /// Spawns an enemy immediately, independently of the spawn timer.
        /// </summary>
        [Export]
        public bool SpawnImmediately { get; set; }

        /// <summary>
        /// The progress ratio the EnemySpawner begins at, or the point along its path that it begins.
        /// Represented by a range of 0 to 1.0, where 0 is at the start of the path and 1.0 is at the end of the path.
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float InitialProgressRatio { get; set; } = 0;

        public SpawnPool SpawnPool => _spawnPool;

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

        [ExportGroup("Spawn Offsets")]
        [Export(PropertyHint.GroupEnable)]
        public bool EnableSpawnOffset { get; set; }

        /// <summary>
        /// Minimum spawn offset factor, in a range from 0 (starts the spawn timer immediately) to 1.0 (starts the spawn timer at the very end of the wave, i.e. never spawns)
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float MinOffset
        {
            get => _minSpawnOffset;
            set => _minSpawnOffset = value;
        }

        /// <summary>
        /// Maximum spawn offset factor, in a range from 0 (starts the spawn timer immediately) to 1.0 (starts the spawn timer at the very end of the wave, i.e. never spawns)
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float MaxOffset
        {
            get => _maxSpawnOffset;
            set => _maxSpawnOffset = value;
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

            // EnemySpawner spawner = _spawnerScene.Instantiate<EnemySpawner>();
            spawner.Name = $"{spawner.GetType().Name}-{Nanoid.Generate(size: 5)}";
            spawner.Curve = curve;
            spawner.Position = position;
            spawner.RotationDegrees = rotationDegrees;
            spawner.Location = _location;
            spawner.SpawnImmediately = SpawnImmediately;
            spawner.InitialProgressRatio = InitialProgressRatio;

            // Set spawn offset
            if (EnableSpawnOffset && waveTime != null)
            {
                // Get the current wave time
                // Get a random value between the min and the max.
                spawner.SpawnTimeOffset = waveTime * RNG.GetRandomDouble(MinOffset, MaxOffset);
            }
        }
    }
}
