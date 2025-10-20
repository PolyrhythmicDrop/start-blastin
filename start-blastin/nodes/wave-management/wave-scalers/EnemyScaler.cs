using Godot;

namespace WaveManagement
{
    /// <summary>
    /// Sets enemy and spawner configuration on a wave-per-wave threshold.
    /// </summary>
    [GlobalClass]
    public partial class EnemyScaler : WaveScaler
    {
        private float _enemySpeedModifier = 0;
        private float _enemyCrashDamageModifier = 0;
        private float _enemyMaxHealthModifier = 0;
        private float _enemyFireRateModifier = 0;
        private float _enemyWeaponDamageModifier = 0;

        [ExportCategory("Stat Modifiers (% Increase)")]
        [Export(PropertyHint.Range, "-100,100,1,suffix:%,or_greater")]
        public float SpeedModifier
        {
            get => _enemySpeedModifier;
            set => _enemySpeedModifier = value;
        }

        [Export(PropertyHint.Range, "-100,100,1,suffix:%,or_greater")]
        public float CrashDamageModifier
        {
            get => _enemyCrashDamageModifier;
            set => _enemyCrashDamageModifier = value;
        }

        [Export(PropertyHint.Range, "0,100,1,suffix:%,or_greater")]
        public float MaxHealthModifier
        {
            get => _enemyMaxHealthModifier;
            set => _enemyMaxHealthModifier = value;
        }

        [ExportCategory("Weapon Modifiers (% Increase)")]
        [Export(PropertyHint.Range, "-100,100,1,suffix:%,or_greater")]
        public float FireRateModifier
        {
            get => _enemyFireRateModifier;
            set => _enemyFireRateModifier = value;
        }

        [Export(PropertyHint.Range, "-100,100,1,suffix:%,or_greater")]
        public float WeaponDamageModifier
        {
            get => _enemyWeaponDamageModifier;
            set => _enemyWeaponDamageModifier = value;
        }

        private void ConvertFromPercent(EnemyScaler scaler)
        {
            scaler.MaxHealthModifier /= 100f;
            scaler.CrashDamageModifier /= 100f;
            scaler.FireRateModifier /= 100f;
            // Square root modifiers: Divide by 10 because the sqrt modifier is small.
            scaler.SpeedModifier /= 10f;
            scaler.WeaponDamageModifier /= 10f;
        }

        private void ApplyDifficultyMultiplier(float difficultyMultiplier)
        {
            SpeedModifier += difficultyMultiplier;
            CrashDamageModifier += difficultyMultiplier;
            MaxHealthModifier += difficultyMultiplier;
            FireRateModifier += difficultyMultiplier;
            WeaponDamageModifier += difficultyMultiplier;
        }

        public EnemyScaler GetAdjustedScaler(float difficultyMod, int wave)
        {
            EnemyScaler adjustedScaler = (EnemyScaler)Duplicate(true);
            ConvertFromPercent(adjustedScaler);

            if (wave == 1)
            {
                return adjustedScaler;
            }
            else
            {
                float difficultyMultiplier = Mathf.Log(1 + wave) * difficultyMod;

                adjustedScaler.ResourceName += "-adjusted";
                adjustedScaler.ApplyDifficultyMultiplier(difficultyMultiplier);

                return adjustedScaler;
            }
        }
    }
}
