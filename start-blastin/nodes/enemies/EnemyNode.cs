using System;
using Autoloads;
using DataStructures;
using Entities;
using Events;
using Factories;
using Godot;
using Interfaces;
using Services;
using Stats;
using Utility;
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
            IWeaponOwner,
            IStats,
            IListener,
            IDeflector
    {
        protected StatManager _stats;

        protected WeaponNode _weapon;

        protected CollisionShape2D _shape;
        protected EntityPath _path;

        protected SoundSet _sounds;

        #region Position and Velocity
        protected Vector2 _currentGlobalPosition;
        protected Vector2 _lastGlobalPosition;
        protected Vector2 _motion => _currentGlobalPosition - _lastGlobalPosition;
        protected Vector2 _lastFramePosition;
        protected Vector2 _currentVelocity = Vector2.Zero;
        protected Tween _followTween;
        #endregion

        #region Stats

        // current stats
        protected float _currentHealth;
        protected float _maxHealth => _stats.GetStat(StatType.MaxHealth).CurrentValue;

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

        #region State
        protected bool _alive = true;

        public bool DeflectActive { get; set; }

        #endregion

        public WeaponNode Weapon => _weapon;
        public EntityPath Path => _path;

        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value;
        }

        public float MaxHealth => _maxHealth;

        #region Init
        public override void _Ready()
        {
            base._Ready();
            AddToGroup("enemies");

            _shape = GetNode<CollisionShape2D>("%CollisionShape2D");

            AddChild(_weapon);

            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;

            // Set an initial firing delay
            double delay = RNG.GetRandomDouble(max: _weapon.Stats.FireRate);
            _weapon.FireTimer.Start(delay);

            // Initialize position tracking
            _lastFramePosition = GlobalPosition;

            ConnectSignals();
        }

        public virtual void ConnectSignals()
        {
            if (_stats != null)
            {
                _stats.StatUpdated += OnStatUpdated;
            }
        }

        public virtual void DisconnectSignals()
        {
            _stats.StatUpdated -= OnStatUpdated;
        }

        /// <summary>
        /// Initializes the enemy node from an enemey resource.
        /// Called from the EnemyFactory before the enemy is added to the scene tree.
        /// </summary>
        /// <param name="enemyResource">The resource used to create the enemy.</param>
        public virtual void Initialize(EnemyResource enemyResource)
        {
            // Health
            _baseMaxHealth = enemyResource.MaxHealth;
            _currentHealth = _baseMaxHealth;

            // Currency
            _fluxReward = enemyResource.FluxReward;
            _byteReward = enemyResource.ByteReward;

            // Weapon initialization
            _weapon = WeaponFactory.CreateWeapon(
                enemyResource.WeaponResource,
                velocityProvider: this,
                owner: this
            );
            _baseFireRate = enemyResource.WeaponResource.Stats.FireRate;
            _baseWeaponDamage = enemyResource.WeaponResource.Stats.Damage;
            _baseSpeed = enemyResource.Speed;
            _baseCrashDamage = enemyResource.CrashDamage;

            // Sound initialization
            _sounds = (SoundSet)enemyResource.Sounds.Duplicate();

            InitializeStatManager();
        }

        /// <summary>
        /// Initializes the enemy's stat manager and base stats.
        /// Called after <see cref="Initialize"/>, before the enemy is added to the scene tree.
        /// </summary>
        public virtual void InitializeStatManager()
        {
            _stats = new();
            _stats.AddStat(StatType.CrashDamage, _baseCrashDamage);
            _stats.AddStat(StatType.Speed, _baseSpeed);
            _stats.AddStat(StatType.FireRate, _baseFireRate);
            _stats.AddStat(StatType.MaxHealth, _baseMaxHealth);
            _stats.AddStat(StatType.Damage, _baseWeaponDamage);
        }

        public void SetPath(EntityPath path)
        {
            _path = path;
        }

        #endregion

        #region Stats and Scaling

        public StatManager GetStatManager()
        {
            return _stats;
        }

        /// <summary>
        /// Sets a stat value based on a passed StatType.
        /// Used for Effects and other objects so you can use the correct getters/setters instead of accessing the StatManager directly.
        /// </summary>
        /// <param name="type">The stat type to set.</param>
        /// <param name="value">The new value for the stat type.</param>
        public virtual void SetStat(StatType type, float value)
        {
            _stats.UpdateStat(type, value);
        }

        public virtual void OnStatUpdated(object source, StatUpdatedEventArgs args)
        {
            switch (args.StatType)
            {
                case StatType.FireRate:
                case StatType.Damage:
                case StatType.ProjectileSpeed:
                    Weapon.UpdateWeaponStats(args.StatType, args.Stat);
                    break;
                case StatType.Speed:
                    if (_followTween != null && _followTween.IsValid())
                    {
                        AdjustFollowSpeed();
                    }
                    else
                    {
                        FollowPath(_path, _followSpeed);
                    }
                    break;
                default:
                    return;
            }
        }

        protected virtual void AdjustFollowSpeed()
        {
            if (_path?.PathFollow == null || _followTween == null)
            {
                return;
            }

            // Get the current progress and calculate the remaining distance
            float currentProgress = _path.PathFollow.ProgressRatio;
            float pathLength = _path.Curve.GetBakedLength();
            float remainingDistance = pathLength * (1.0f - currentProgress);

            // Calculate the new duration based on the current _followSpeed.
            float duration = Math.Max(remainingDistance / _followSpeed, 0.1f);

            // Store whether the current tween was paused so we can re-pause it after creating the new one.
            bool wasPaused = !_followTween.IsRunning();

            // Kill and recreate the existing tween
            _followTween.Kill();

            _followTween = CreateTween();
            _followTween.TweenProperty(_path.PathFollow, "progress_ratio", 1.0, duration);

            // Pause the new tween if the original tween was paused.
            if (wasPaused)
            {
                _followTween.Pause();
            }
        }

        /// <summary>
        /// Adjusts the enemy's stats based on the current wave and the selected enemy scaler.
        /// </summary>
        /// <param name="scaler">The EnemyScaler object to scale using.</param>
        /// <param name="wave">The current wave.</param>
        /// <remarks>
        /// Called from the <see cref="EnemySpawner"/> after the enemy has been built by the <see cref="EnemyFactory"/> and initialized.
        /// </remarks>
        public virtual void ApplyWaveScaling(EnemyScaler scaler, int wave)
        {
            // Don't apply scaling on the first wave.
            if (wave == 1)
            {
                return;
            }

            float waveLogMultiplier = Mathf.Log(1 + wave);
            float waveSqrtMultiplier = Mathf.Sqrt(wave) * 0.1f;

            float newMaxHealth =
                _baseMaxHealth * (1 + (scaler.MaxHealthModifier * waveLogMultiplier));
            SetStat(StatType.MaxHealth, newMaxHealth);

            float newCrashDamage =
                _baseCrashDamage * (1 + (scaler.CrashDamageModifier * waveLogMultiplier));
            SetStat(StatType.CrashDamage, newCrashDamage);

            float newFollowSpeed = _baseSpeed * (1 + (scaler.SpeedModifier * waveSqrtMultiplier));
            SetStat(StatType.Speed, newFollowSpeed);

            float newDamage =
                _baseWeaponDamage * (1 + (scaler.WeaponDamageModifier * waveSqrtMultiplier));
            SetStat(StatType.Damage, newDamage);

            float waveExpoMultiplier = Mathf.Pow(0.95f, wave * scaler.FireRateModifier);
            // Fire rate should be decreased, since lower fire rates result in faster firing.
            float newFireRate = Mathf.Max(0.1f, _baseFireRate * waveExpoMultiplier);
            // _weapon.Stats.FireRate = Mathf.Max(0.1f, _baseFireRate * waveExpoMultiplier);
            SetStat(StatType.FireRate, newFireRate);
        }

        #endregion

        #region Actions

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
            if (_alive)
            {
                PlayDamageAnimation();
                IndicatorFactory.CreateTextIndicator(
                    (MathF.Round(damage, 1) * -1).ToString(),
                    GlobalPosition,
                    parent: this
                );
                _currentHealth -= damage;

                if (_currentHealth <= 0)
                {
                    _currentHealth = 0;
                    Die(playerId);
                }
            }
        }

        public void Heal(float healAmount)
        {
            // Don't do anything if current health is greater than max health
            if (_currentHealth >= _maxHealth)
            {
                return;
            }
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        }

        protected virtual void FireWeapon()
        {
            AudioService.Instance.PlaySound(_sounds?.Fire, this, 1);
            _weapon.Fire();
        }

        public virtual async void Die(int? playerId = null)
        {
            if (playerId != null)
            {
                EnemyKilledEventArgs args = new(
                    (int)playerId,
                    _fluxReward,
                    _byteReward,
                    GlobalPosition
                );
                EventBus.Instance.RaiseEnemyKilled(args);
            }
            // Queue free after all child projectiles die
            bool projectilesDisabled = await _weapon.WaitForAllProjectilesDisabled();
            if (projectilesDisabled)
            {
                QueueFree();
            }
        }

        public virtual void PlayDamageAnimation()
        {
            string mixRatioPath = "mix_ratio";
            string currentFramePath = "current_frame";

            if (Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter(mixRatioPath, 1.0);

                Tween tween = CreateTween();
                tween.TweenMethod(
                    Callable.From(
                        (int currentFrame) =>
                            shaderMaterial.SetShaderParameter(currentFramePath, currentFrame)
                    ),
                    0,
                    30,
                    0.5
                );
                tween.TweenCallback(
                    Callable.From(() => shaderMaterial.SetShaderParameter(mixRatioPath, 0))
                );
            }
        }

        public virtual void OnCrash(KinematicCollision2D collision)
        {
            if (collision.GetCollider() is Player player && _alive)
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

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            _followTween = CreateTween();
            _followTween.TweenProperty(path.PathFollow, "progress_ratio", 1.0, duration);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }

        #endregion
    }
}
