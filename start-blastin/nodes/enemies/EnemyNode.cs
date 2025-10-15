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
    public abstract partial class EnemyNode : AnimatableBody2D, IDie, IHealthful
    {
        protected HealthComponent _healthComponent;
        protected WeaponNode _weapon;
        protected float _speed;
        protected float _crashDamage;
        protected CollisionShape2D _shape;
        protected EntityPath _path;
        protected EnemyState _state;

        public HealthComponent HealthComp => _healthComponent;
        public WeaponNode Weapon => _weapon;
        public EntityPath Path => _path;

        public void TakeDamage(float damage)
        {
            PlayDamageAnimation();
            _healthComponent.TakeDamage(damage);
        }

        public void Heal(float healAmount) => _healthComponent.Heal(healAmount);

        public virtual void Initialize(EnemyResource enemyResource)
        {
            _healthComponent = enemyResource.HealthComponent;
            _healthComponent.Initialize(this);
            _weapon = WeaponFactory.CreateWeapon(enemyResource.WeaponResource, true);
            _speed = enemyResource.Speed;
            _crashDamage = enemyResource.CrashDamage;
        }

        public virtual void ApplyWaveConfig(EnemyWaveConfig config)
        {
            _healthComponent.MaxHealth += config.MaxHealthModifier * _healthComponent.MaxHealth;
            _crashDamage += config.CrashDamageModifier * _crashDamage;
            _speed += config.SpeedModifier * _speed;
            _weapon.Stats.FireRate += config.FireRateModifier * _weapon.Stats.FireRate;
            _weapon.Stats.Damage += config.WeaponDamageModifier * _weapon.Stats.Damage;

            GD.Print(
                $"{MethodBase.GetCurrentMethod().Name}: Wave Config {config.ResourceName} applied! New stats:\nMaxHealth: {_healthComponent.MaxHealth} | Crash Damage {_crashDamage} | Speed {_speed}\nFire Rate {_weapon.Stats.FireRate} | Damage {_weapon.Stats.Damage}"
            );
        }

        public void SetPath(EntityPath path)
        {
            _path = path;
        }

        public override void _Ready()
        {
            base._Ready();

            _shape = GetNode<CollisionShape2D>("CollisionShape2D");

            AddChild(_weapon);

            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;
            _weapon.FireTimer.Start();

            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Following path of {_path.Name}..."
            // );
            _path.FollowPath(_speed);
        }

        protected virtual void FireWeapon()
        {
            _weapon.Fire();
        }

        public virtual void Die()
        {
            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            // );

            // Queue free after all child projectiles die
            _weapon.AllProjectilesDisabled += QueueFree;
            // QueueFree();
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
