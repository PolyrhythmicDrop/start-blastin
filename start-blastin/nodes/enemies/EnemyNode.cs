using System.Reflection;
using Components;
using Entities;
using Factories;
using Godot;
using Interfaces;
using WaveManagement;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public abstract partial class EnemyNode : AnimatableBody2D, IDie, IHealthful, IVelocityProvider
    {
        protected HealthComponent _healthComponent;
        protected WeaponNode _weapon;

        /// <summary>
        /// The speed at which this enemy follows its assigned path.
        /// </summary>
        protected float _followSpeed;
        protected float _crashDamage;
        protected CollisionShape2D _shape;
        protected EntityPath _path;
        protected EnemyState _state;

        protected Vector2 _currentGlobalPosition;
        protected Vector2 _lastGlobalPosition;
        protected Vector2 _motion => _currentGlobalPosition - _lastGlobalPosition;

        // Base stats
        protected float _baseSpeed;
        protected float _baseCrashDamage;
        protected float _baseMaxHealth;
        protected float _baseFireRate;
        protected float _baseWeaponDamage;

        public HealthComponent HealthComp => _healthComponent;
        public WeaponNode Weapon => _weapon;
        public EntityPath Path => _path;

        public override void _Ready()
        {
            base._Ready();
            AddToGroup("enemies");

            _shape = GetNode<CollisionShape2D>("CollisionShape2D");

            AddChild(_weapon);

            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;
            _weapon.FireTimer.Start();

            _path.FollowPath(_followSpeed);
        }

        public Vector2 GetCurrentVelocity()
        {
            return _motion;
        }

        public void TakeDamage(float damage)
        {
            PlayDamageAnimation();
            _healthComponent.TakeDamage(damage);
        }

        public void Heal(float healAmount) => _healthComponent.Heal(healAmount);

        public virtual void Initialize(EnemyResource enemyResource)
        {
            _healthComponent = (HealthComponent)enemyResource.HealthComponent.Duplicate();
            _healthComponent.Initialize(this);
            _baseMaxHealth = _healthComponent.MaxHealth;

            _weapon = WeaponFactory.CreateWeapon(
                enemyResource.WeaponResource,
                true,
                velocityProvider: this
            );
            _baseFireRate = enemyResource.WeaponResource.Stats.FireRate;
            _baseWeaponDamage = enemyResource.WeaponResource.Stats.Damage;

            _followSpeed = enemyResource.Speed;
            _baseSpeed = enemyResource.Speed;

            _crashDamage = enemyResource.CrashDamage;
            _baseCrashDamage = enemyResource.CrashDamage;
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

            _healthComponent.MaxHealth =
                _baseMaxHealth * (1 + (scaler.MaxHealthModifier * waveLogMultiplier));

            _crashDamage =
                _baseCrashDamage * (1 + (scaler.CrashDamageModifier * waveLogMultiplier));

            _followSpeed = _baseSpeed * (1 + (scaler.SpeedModifier * waveSqrtMultiplier));
            _weapon.Stats.Damage =
                _baseWeaponDamage * (1 + (scaler.WeaponDamageModifier * waveSqrtMultiplier));

            float waveExpoMultiplier = Mathf.Pow(0.95f, wave * scaler.FireRateModifier);
            // Fire rate should be decreased, since lower fire rates result in faster firing.
            _weapon.Stats.FireRate = Mathf.Max(0.1f, _baseFireRate * waveExpoMultiplier);

            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().Name}: Wave Config {scaler.ResourceName} applied! New stats:\nMaxHealth: {_healthComponent.MaxHealth} | Crash Damage {_crashDamage} | Speed {_speed}\nFire Rate {_weapon.Stats.FireRate} | Damage {_weapon.Stats.Damage}"
            // );
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
            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            // );

            // Queue free after all child projectiles die
            bool projectilesDisabled = await _weapon.WaitForAllProjectilesDisabled();
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}: Projectiles disabled? {projectilesDisabled}"
            );
            QueueFree();
        }

        public virtual void PlayDamageAnimation() { }

        public virtual void OnCrash(KinematicCollision2D collision)
        {
            if (collision.GetCollider() is Player player)
            {
                GD.Print($"{player.Name} was crashed into! Player takes {_crashDamage} damage.");
                player.TakeDamage(_crashDamage);
                Die();
            }
        }
    }
}
