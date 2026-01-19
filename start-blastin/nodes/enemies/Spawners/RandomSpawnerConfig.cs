using System.Linq;
using Autoloads;
using Godot;
using NanoidDotNet;

namespace Enemies.Spawners
{
    /// <summary>
    /// Resource for generating concrete RandomSpawner objects.
    /// </summary>
    [GlobalClass]
    public partial class RandomSpawnerConfig : SpawnerConfig
    {
        private float _minSpawnDelay;
        private float _maxSpawnDelay;

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

        [Export]
        public Godot.Collections.Dictionary<SpawnData, int> SpawnPool { get; set; }

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

        public override void ConfigureSpawner(EnemySpawner spawner, double? waveTime = null)
        {
            if (spawner is not RandomSpawner randomSpawner)
            {
                return;
            }

            base.ConfigureSpawner(randomSpawner, waveTime);
            ConfigureRandomSpawner(randomSpawner, waveTime);
        }

        private void ConfigureRandomSpawner(RandomSpawner spawner, double? waveTime = null)
        {
            spawner.SpawnImmediately = SpawnImmediately;
            spawner.StartMoveOnSpawnTimer = StartMoveOnSpawnTimer;
            spawner.InitialProgressRatio = InitialProgressRatio;
            spawner.SpawnPool = SpawnPool.ToDictionary();

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
