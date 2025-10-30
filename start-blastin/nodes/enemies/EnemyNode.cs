using System.Reflection;
using Components;
using Entities;
using Factories;
using Godot;
using Interfaces;
using Stats;
using WaveManagement;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public abstract partial class EnemyNode
        : AnimatableBody2D,
            IDie,
            IHealthful,
            IVelocityProvider,
            IWeaponOwner
    {
        protected StatManager _stats;

        // protected HealthComponent _healthComponent;
        protected WeaponNode _weapon;

        /// <summary>
        /// The speed at which this enemy follows its assigned path.
        /// </summary>
        protected float _followSpeed => _stats.GetStat(StatType.Speed).CurrentValue;

        protected float _crashDamage => _stats.GetStat(StatType.CrashDamage).CurrentValue;
        protected CollisionShape2D _shape;
        protected EntityPath _path;
        protected EnemyState _state;

        protected Vector2 _currentGlobalPosition;
        protected Vector2 _lastGlobalPosition;
        protected Vector2 _motion => _currentGlobalPosition - _lastGlobalPosition;

        #region Stats

        // current stats
        protected float _currentHealth;
        protected float _maxHealth;

        // Base stats
        protected float _baseSpeed;
        protected float _baseCrashDamage;
        protected float _baseMaxHealth;
        protected float _baseFireRate;
        protected float _baseWeaponDamage;
        protected int _fluxReward;
        protected int _byteReward;

        #endregion

        // public HealthComponent HealthComp => _healthComponent;
        public WeaponNode Weapon => _weapon;
        public EntityPath Path => _path;

        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value;
        }

        public override void _Ready()
        {
            base._Ready();
            AddToGroup("enemies");

            _shape = GetNode<CollisionShape2D>("CollisionShape2D");

            AddChild(_weapon);

            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;
            _weapon.FireTimer.Start(_weapon.Stats.FireRate);

            _path.FollowPath(_followSpeed);
        }

        public Vector2 GetCurrentVelocity()
        {
            return _motion;
        }

        public void TakeDamage(float damage)
        {
            PlayDamageAnimation();
            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
            // _healthComponent.TakeDamage(damage);
        }

        public void Heal(float healAmount)
        {
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        }

        public virtual void Initialize(EnemyResource enemyResource)
        {
            // _healthComponent = (HealthComponent)enemyResource.HealthComponent.Duplicate();
            // _healthComponent.Initialize(this);
            _baseMaxHealth = enemyResource.HealthComponent.MaxHealth;
            _maxHealth = _baseMaxHealth;
            _currentHealth = _baseMaxHealth;

            _weapon = WeaponFactory.CreateWeapon(
                enemyResource.WeaponResource,
                true,
                velocityProvider: this,
                owner: this
            );
            _baseFireRate = enemyResource.WeaponResource.Stats.FireRate;
            _baseWeaponDamage = enemyResource.WeaponResource.Stats.Damage;
            _baseSpeed = enemyResource.Speed;
            _baseCrashDamage = enemyResource.CrashDamage;

            _fluxReward = enemyResource.FluxReward;
            _byteReward = enemyResource.ByteReward;

            InitializeStats();
        }

        public virtual void InitializeStats()
        {
            _stats = new();
            _stats.AddStat(StatType.CrashDamage, _baseCrashDamage);
            _stats.AddStat(StatType.Speed, _baseSpeed);
        }

        public virtual void ApplyWaveScaling(EnemyScaler scaler, int wave)
        {
            // Don't apply scaling on the first wave.
            if (wave == 1)
            {
                return;
            }

            float waveLogMultiplier = Mathf.Log(1 + wave);
            float waveSqrtMultiplier = Mathf.Sqrt(wave) * 0.1f;

            _maxHealth = _baseMaxHealth * (1 + (scaler.MaxHealthModifier * waveLogMultiplier));

            float newCrashDamage =
                _baseCrashDamage * (1 + (scaler.CrashDamageModifier * waveLogMultiplier));
            _stats.UpdateStat(StatType.CrashDamage, newCrashDamage);

            float newFollowSpeed = _baseSpeed * (1 + (scaler.SpeedModifier * waveSqrtMultiplier));
            _stats.UpdateStat(StatType.Speed, newFollowSpeed);

            _weapon.Stats.Damage =
                _baseWeaponDamage * (1 + (scaler.WeaponDamageModifier * waveSqrtMultiplier));

            float waveExpoMultiplier = Mathf.Pow(0.95f, wave * scaler.FireRateModifier);
            // Fire rate should be decreased, since lower fire rates result in faster firing.
            _weapon.Stats.FireRate = Mathf.Max(0.1f, _baseFireRate * waveExpoMultiplier);
        }

        public void SetPath(EntityPath path)
        {
            _path = path;
        }

        protected virtual void FireWeapon()
        {
            _weapon.Fire();
        }

        public virtual async void Die()
        {
            // Queue free after all child projectiles die
            bool projectilesDisabled = await _weapon.WaitForAllProjectilesDisabled();
            if (projectilesDisabled)
            {
                QueueFree();
            }
        }

        public virtual void PlayDamageAnimation() { }

        public virtual void OnCrash(KinematicCollision2D collision)
        {
            if (collision.GetCollider() is Player player)
            {
                // float crashDamage = _stats.GetStat(StatType.CrashDamage).Value;
                GD.Print($"{player.Name} was crashed into! Player takes {_crashDamage} damage.");
                player.TakeDamage(_crashDamage);
                // player.TakeDamage(crashDamage);
                Die();
            }
        }
    }
}
