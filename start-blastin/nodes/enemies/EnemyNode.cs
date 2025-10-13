using System.Reflection;
using Components;
using Factories;
using Godot;
using Interfaces;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public abstract partial class EnemyNode : AnimatableBody2D, IDie, IHealthful
    {
        protected HealthComponent _healthComponent;
        protected WeaponNode _weapon;
        protected float _speed;
        protected CollisionShape2D _shape;

        protected EntityPath _path;

        protected EnemyState _state;

        public HealthComponent HealthComp => _healthComponent;
        public WeaponNode Weapon => _weapon;

        public EntityPath Path => _path;

        public void TakeDamage(int damage) => _healthComponent.TakeDamage(damage);

        public void Heal(int healAmount) => _healthComponent.Heal(healAmount);

        public virtual void Initialize(EnemyResource enemyResource)
        {
            _healthComponent = (HealthComponent)enemyResource.HealthComponent.Duplicate(true);
            _healthComponent.Initialize(this);

            _weapon = WeaponFactory.CreateWeapon(enemyResource.WeaponResource, true);
            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Weapon created for {Name}! Weapon: {_weapon.Name}"
            // );
            // _curve = enemyResource.PathCurve;
            _speed = enemyResource.Speed;
        }

        public void SetPath(EntityPath path)
        {
            _path = path;
        }

        public override void _Ready()
        {
            base._Ready();
            // _path = GetNode<Path2D>("%Path2D");
            // _path.Curve = _curve;
            // _pathFollow = _path.GetNode<PathFollow2D>("%PathFollow2D");

            _shape = GetNode<CollisionShape2D>("CollisionShape2D");

            AddChild(_weapon);

            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;
            _weapon.FireTimer.Start();

            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Following path of {_path.Name}..."
            );
            _path.FollowPath(_speed);
        }

        protected virtual void FireWeapon()
        {
            _weapon.Fire();
        }

        public virtual void Die()
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            );
            QueueFree();
        }
    }
}
