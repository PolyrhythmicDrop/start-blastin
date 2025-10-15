using Godot;

namespace WaveManagement
{
    /// <summary>
    /// Sets enemy and spawner configuration on a wave-per-wave threshold.
    /// </summary>
    [GlobalClass]
    public partial class EnemyWaveConfig : Resource
    {
        private int _minWave = 1;
        private int _maxWave = -1;

        private float _enemySpeedModifier;
        private float _enemyCrashDamageModifier;
        private float _enemyMaxHealthModifier;
        private float _enemyFireRateModifier;
        private float _enemyWeaponDamageModifier;

        [ExportCategory("Wave Thresholds")]
        [Export]
        public int MinWave
        {
            get => _minWave;
            set => _minWave = value;
        }

        [Export]
        public int MaxWave
        {
            get => _maxWave;
            set => _maxWave = value;
        }

        [ExportCategory("Enemy Stat Modifiers")]
        [Export]
        public float SpeedModifier
        {
            get => _enemySpeedModifier;
            set => _enemySpeedModifier = value;
        }

        [Export]
        public float CrashDamageModifier
        {
            get => _enemyCrashDamageModifier;
            set => _enemyCrashDamageModifier = value;
        }

        [Export]
        public float MaxHealthModifier
        {
            get => _enemyMaxHealthModifier;
            set => _enemyMaxHealthModifier = value;
        }

        [ExportCategory("Enemy Weapon Modifiers")]
        [Export]
        public float FireRateModifier
        {
            get => _enemyFireRateModifier;
            set => _enemyFireRateModifier = value;
        }

        [Export]
        public float WeaponDamageModifier
        {
            get => _enemyWeaponDamageModifier;
            set => _enemyWeaponDamageModifier = value;
        }

        /// <summary>
        /// Augments the wave configuration modifiers with the current difficulty modifier.
        /// </summary>
        /// <param name="difficultyMod"></param>
        public void ApplyDifficultyModifier(float difficultyMod)
        {
            _enemySpeedModifier += difficultyMod;
            _enemyCrashDamageModifier += difficultyMod;
            _enemyMaxHealthModifier += difficultyMod;
            _enemyFireRateModifier += difficultyMod;
            _enemyWeaponDamageModifier += difficultyMod;
        }
    }
}
