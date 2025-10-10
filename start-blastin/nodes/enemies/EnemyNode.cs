using System.Reflection;
using Components;
using Factories;
using Godot;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public abstract partial class EnemyNode : Node2D
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

        public virtual void Initialize(EnemyResource enemyResource)
        {
            _healthComponent = enemyResource.HealthComponent;
            _weapon = WeaponFactory.CreateWeapon(enemyResource.WeaponResource);
            _curve = enemyResource.PathCurve;
            _speed = enemyResource.Speed;
        }

        public override void _Ready()
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            );

            base._Ready();
            _path = GetNode<Path2D>("%Path2D");
            _path.Curve = _curve;

            _pathFollow = _path.GetNode<PathFollow2D>("%PathFollow2D");
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: _pathFollow initial rotation: {_pathFollow.Rotation}"
            );
            _characterBody = _pathFollow.GetNode<Area2D>("%DroneBody");

            _characterBody.AddChild(_weapon);

            FollowPath();
        }

        // protected virtual void SetState()
        // {
        //     EnemyMoveState moveState;
        //     EnemyFireState fireState;

        //     if (_characterBody.Velocity != Vector2.Zero)
        //     {
        //         moveState = EnemyMoveState.Moving;
        //     }
        //     else
        //     {
        //         moveState = EnemyMoveState.Idle;
        //     }

        //     if (_weapon.FireTimer.IsStopped())
        //     {
        //         fireState = EnemyFireState.Hold;
        //     }
        //     else
        //     {
        //         fireState = EnemyFireState.Fire;
        //     }

        //     _state = new EnemyState(moveState, fireState);
        // }

        protected virtual void FireWeapon()
        {
            _weapon.Fire();
        }

        protected virtual void FollowPath()
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: setting duration and following path."
            );
            float pathLength = _curve.GetBakedLength();
            float duration = pathLength / _speed;

            GD.Print(
                $"{Name} tweener stats:\nSpeed: {_speed} | Path Length: {pathLength} | Duration: {duration}"
            );

            Tween tween = CreateTween();
            tween.TweenProperty(_pathFollow, "progress_ratio", 1.0, duration);
        }

        protected void PrintRotation()
        {
            GD.Print($"{Name}.{_pathFollow.Name} rotation: {_pathFollow.Rotation}");
            GD.Print($"{Name}.{_characterBody.Name} rotation: {_characterBody.Rotation}");
        }
    }
}
