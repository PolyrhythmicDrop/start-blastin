using Godot;

namespace WaveManagement
{
    /// <summary>
    /// Sets enemy and spawner configuration on a wave-per-wave threshold.
    /// </summary>
    [GlobalClass]
    public partial class EnemyScaler : WaveScaler
    {
        private float _enemySpeedModifier;
        private float _enemyCrashDamageModifier;
        private float _enemyMaxHealthModifier;
        private float _enemyFireRateModifier;
        private float _enemyWeaponDamageModifier;

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
        public override void ApplyDifficultyModifier(float difficultyMod)
        {
            // _enemySpeedModifier += difficultyMod;
            // _enemyCrashDamageModifier += difficultyMod;
            // _enemyMaxHealthModifier += difficultyMod;
            // _enemyFireRateModifier += difficultyMod;
            // _enemyWeaponDamageModifier += difficultyMod;

            // GD.Print($"Difficulty modifier applied to {ResourceName}!");
        }

        public EnemyScaler GetAdjustedScaler(float difficultyMod, int wave)
        {
            float difficultyMultiplier = Mathf.Log(1 + wave) * difficultyMod;
            if (wave == 1)
            {
                return this;
            }
            else
            {
                return new EnemyScaler
                {
                    ResourceName = this.ResourceName + "-adjusted",
                    SpeedModifier = this._enemySpeedModifier + difficultyMultiplier,
                    CrashDamageModifier = this.CrashDamageModifier + difficultyMultiplier,
                    MaxHealthModifier = this.MaxHealthModifier + difficultyMultiplier,
                    FireRateModifier = this.FireRateModifier + difficultyMultiplier,
                    WeaponDamageModifier = this.WeaponDamageModifier + difficultyMultiplier,
                    MinWave = this._minWave,
                    MaxWave = this._maxWave,
                };
            }
        }
    }
}
