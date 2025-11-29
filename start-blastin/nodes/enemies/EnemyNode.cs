using System.Reflection;
using Autoloads;
using Components;
using Entities;
using Events;
using Factories;
using Godot;
using Interfaces;
using Microsoft.VisualBasic;
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

        protected CollisionShape2D _shape;
        protected EntityPath _path;

        #region Position and Velocity
        protected Vector2 _currentGlobalPosition;
        protected Vector2 _lastGlobalPosition;
        protected Vector2 _motion => _currentGlobalPosition - _lastGlobalPosition;
        protected Vector2 _lastFramePosition;
        protected Vector2 _currentVelocity = Vector2.Zero;
        #endregion

        #region Stats

        // current stats
        protected float _currentHealth;
        protected float _maxHealth;

        /// <summary>
        /// The speed at which this enemy follows its assigned path.
        /// </summary>
        protected float _followSpeed => _stats.GetStat(StatType.Speed).CurrentValue;

        protected float _crashDamage => _stats.GetStat(StatType.CrashDamage).CurrentValue;

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

            // Set an initial firing delay
            float delay = (float)GD.RandRange(0, _weapon.Stats.FireRate);
            _weapon.FireTimer.Start(delay);

            // Initialize position tracking
            _lastFramePosition = GlobalPosition;
        }

        public Vector2 GetCurrentVelocity()
        {
            return _currentVelocity;
        }

        /// <summary>
        /// Causes this enemy node to take damage.
        /// </summary>
        /// <param name="damage">The amount of damage to take.</param>
        /// <param name="playerId">If a player caused the damage, the <see cref="Player.PlayerId"/> of the damaging player.</param>
        public void TakeDamage(float damage, int? playerId = null)
        {
            PlayDamageAnimation();
            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die(playerId);
            }
        }

        public void Heal(float healAmount)
        {
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        }

        public virtual void Initialize(EnemyResource enemyResource)
        {
            Name = enemyResource.ResourceName + DateAndTime.Now.Ticks;
            // _healthComponent = (HealthComponent)enemyResource.HealthComponent.Duplicate();
            // _healthComponent.Initialize(this);
            _baseMaxHealth = enemyResource.HealthComponent.MaxHealth;
            _maxHealth = _baseMaxHealth;
            _currentHealth = _baseMaxHealth;

            _weapon = WeaponFactory.CreateWeapon(
                enemyResource.WeaponResource,
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

        public virtual async void Die(int? playerId = null)
        {
            if (playerId != null)
            {
                EnemyKilledEventArgs args = new((int)playerId, _fluxReward, _byteReward);
                EventBus.Instance.RaiseEnemyKilled(args);
            }
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
                player.TakeDamage(_crashDamage);
                Die();
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (delta > 0)
            {
                _currentVelocity = (GlobalPosition - _lastFramePosition) / (float)delta;
            }

            _lastFramePosition = GlobalPosition;
        }

        /// <summary>
        /// Follows an EntityPath at a set speed.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="speed"></param>
        protected virtual void FollowPath(EntityPath path, float speed)
        {
            float pathLength = path.Curve.GetBakedLength();
            float duration = Mathf.Max(pathLength / speed, 0.1f);

            Tween tween = CreateTween();
            tween.TweenProperty(path.PathFollow, "progress_ratio", 1.0, duration);
        }
    }
}
