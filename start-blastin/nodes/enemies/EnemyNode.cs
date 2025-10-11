using System.Reflection;
using Components;
using Factories;
using Godot;
using Interfaces;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public abstract partial class EnemyNode : Node2D, IDie, IHealthful
    {
        protected HealthComponent _healthComponent;
        protected WeaponNode _weapon;
        protected float _speed;
        protected Curve2D _curve;
        protected Path2D _path;
        protected PathFollow2D _pathFollow;
        protected Area2D _characterBody;

        protected EnemyState _state;

        public HealthComponent HealthComp => _healthComponent;
        public WeaponNode Weapon => _weapon;

        public Path2D Path => _path;

        public void TakeDamage(int damage) => _healthComponent.TakeDamage(damage);

        public void Heal(int healAmount) => _healthComponent.Heal(healAmount);

        public virtual void Initialize(EnemyResource enemyResource)
        {
            _healthComponent = enemyResource.HealthComponent;
            _healthComponent.Owner = this;
            _weapon = WeaponFactory.CreateWeapon(enemyResource.WeaponResource, true);
            _curve = enemyResource.PathCurve;
            _speed = enemyResource.Speed;
        }

        public override void _Ready()
        {
            base._Ready();
            _path = GetNode<Path2D>("%Path2D");
            _path.Curve = _curve;

            _pathFollow = _path.GetNode<PathFollow2D>("%PathFollow2D");
            _characterBody = _pathFollow.GetNode<Area2D>("%DroneBody");

            _characterBody.AddChild(_weapon);

            FollowPath();
        }

        protected virtual void FireWeapon()
        {
            _weapon.Fire();
        }

        protected virtual void FollowPath()
        {
            float pathLength = _curve.GetBakedLength();
            float duration = pathLength / _speed;

            Tween tween = CreateTween();
            tween.TweenProperty(_pathFollow, "progress_ratio", 1.0, duration);
        }

        public virtual void Die()
        {
            // Do the dying
        }
    }
}
