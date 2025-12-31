using System;
using Enemies;
using Enemies.Spawners;
using Godot;

namespace WaveManagement
{
    /// <summary>
    /// Adjusts all EnemySpawner's movement speed and spawn interval based on the current wave.
    /// </summary>
    [GlobalClass]
    public partial class SpawnerScaler : WaveScaler
    {
        private float _moveDurationModifier;
        private float _spawnIntervalModifier;

        /// <summary>
        /// Modifies the spawner's <see cref="EnemySpawner.SpawnPointMoveDuration"/> variable, which determines the speed of the spawn point's movement.
        /// The higher the value, the faster the EnemySpawner moves.
        /// </summary>
        [Export]
        public float MoveDurationModifier
        {
            get => _moveDurationModifier;
            set => _moveDurationModifier = value;
        }

        /// <summary>
        /// Modifies the spawner's <see cref="EnemySpawner.SpawnInterval"/> variable, which determines the interval of time between enemy spawns.
        /// The higher the value, the more frequently the EnemySpawner spawns enemies.
        /// </summary>
        [Export]
        public float SpawnIntervalModifier
        {
            get => _spawnIntervalModifier;
            set => _spawnIntervalModifier = value;
        }

        public SpawnerScaler GetAdjustedScaler(float difficultyMod, int wave)
        {
            if (wave == 1)
            {
                return this;
            }

            float difficultyPercentage = difficultyMod * 100f;
            float difficultyScale = Mathf.Sqrt(wave) * difficultyPercentage * 0.1f;

            return new SpawnerScaler
            {
                ResourceName = this.ResourceName + "-adjusted",
                MoveDurationModifier = Mathf.Min(80f, this._moveDurationModifier + difficultyScale),
                SpawnIntervalModifier = Mathf.Min(
                    80f,
                    this._spawnIntervalModifier + difficultyScale
                ),
                MinWave = this._minWave,
                MaxWave = this._maxWave,
            };
        }
    }
}
