using System;
using Enemies;
using Enemies.Spawners;
using Godot;

namespace WaveManagement
{
    [GlobalClass]
    public partial class SpawnerScaler : WaveScaler
    {
        private SpawnPool _spawnPool = new();
        private float _moveDurationModifier;
        private float _spawnIntervalModifier;

        [Export]
        public Godot.Collections.Array<SpawnData> SpawnPool
        {
            get => _spawnPool.ConvertToGodotArray();
            set => _spawnPool = new SpawnPool(value);
        }

        /// <summary>
        /// Modifies the spawner's <see cref="EnemySpawner.SpawnPointMoveDuration"/> variable, which determines the speed of the spawn point's movement.
        /// </summary>
        [Export]
        public float MoveDurationModifier
        {
            get => _moveDurationModifier;
            set => _moveDurationModifier = value;
        }

        /// <summary>
        /// Modifies the spawner's <see cref="EnemySpawner.SpawnInterval"/> variable, which determines the interval of time between enemy spawns.
        /// </summary>
        [Export]
        public float SpawnIntervalModifier
        {
            get => _spawnIntervalModifier;
            set => _spawnIntervalModifier = value;
        }

        public override void ApplyDifficultyModifier(float difficultyMod)
        {
            // _moveDurationModifier += difficultyMod;
            // _spawnIntervalModifier += difficultyMod;
        }

        public SpawnerScaler GetAdjustedScaler(float difficultyMod, int wave)
        {
            float difficultyMultiplier = Mathf.Sqrt(wave) * difficultyMod * 0.1f;

            if (wave == 1)
            {
                return this;
            }
            else
            {
                return new SpawnerScaler
                {
                    ResourceName = this.ResourceName + "-adjusted",
                    SpawnPool = this._spawnPool.ConvertToGodotArray(),
                    MoveDurationModifier = Mathf.Min(
                        0.2f,
                        this._moveDurationModifier + difficultyMultiplier
                    ),
                    SpawnIntervalModifier = Mathf.Min(
                        0.1f,
                        this._spawnIntervalModifier + difficultyMultiplier
                    ),
                    MinWave = this._minWave,
                    MaxWave = this._maxWave,
                };
            }
        }
    }
}
