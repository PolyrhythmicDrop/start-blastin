using System;
using System.ComponentModel.Design;
using Effects;
using Godot;
using Interfaces;
using Utility;

namespace Weapons
{
    [GlobalClass]
    public partial class TurretBarrel : Barrel
    {
        private TurretEffect.DynamicDirection _dynamicDir;
        private TurretEffect.TargetObject _targetObjectType;
        private IWeaponOwner _weaponOwner;
        private float _rotateDuration;
        private bool _rotationStarted;

        public Vector2 TargetDirection { get; set; }

        public Node2D TargetObject { get; set; } = null;

        public TurretBarrel(BarrelDirection direction)
            : base(direction) { }

        public void SetDynamicDirection(TurretEffect.DynamicDirection dir)
        {
            _dynamicDir = dir;
        }

        public void SetWeaponOwner(IWeaponOwner owner)
        {
            _weaponOwner = owner;
        }

        public void SetTargetObjectType(
            TurretEffect.TargetObject targetType = TurretEffect.TargetObject.None
        )
        {
            _targetObjectType = targetType;
        }

        public void SetRotateDuration(float duration)
        {
            _rotateDuration = duration;
        }

        public void SetTargetObject(Node2D target = null)
        {
            if (target != null)
            {
                TargetObject = target;
            }
            else
            {
                TargetObject = _targetObjectType switch
                {
                    TurretEffect.TargetObject.Nearest => EnemyFinder.GetClosestEnemy(GlobalPosition)
                        ?? null,
                    TurretEffect.TargetObject.LeastHealthy => EnemyFinder.GetLeastHealthyEnemy()
                        ?? null,
                    TurretEffect.TargetObject.StrongestAttack => EnemyFinder.GetStrongestEnemy()
                        ?? null,
                    _ => null,
                };
            }
        }

        public override void _Process(double delta)
        {
            if (_dynamicDir == TurretEffect.DynamicDirection.None || Active == false)
            {
                return;
            }

            if (_dynamicDir == TurretEffect.DynamicDirection.TargetObject)
            {
                SetTargetObject();
            }

            if (_dynamicDir != TurretEffect.DynamicDirection.TimedRotate)
            {
                SetTargetDirection();
                RotateTurret(TargetDirection);
            }
            else if (!_rotationStarted)
            {
                StartTimedRotation(_rotateDuration);
            }
        }

        private void SetTargetDirection()
        {
            TargetDirection = _dynamicDir switch
            {
                TurretEffect.DynamicDirection.Movement => _weaponOwner is IVelocityProvider velocity
                    ? velocity.GetCurrentVelocity().Normalized()
                    : Vector2.Zero,
                TurretEffect.DynamicDirection.MovementOpposite => _weaponOwner
                    is IVelocityProvider velocity
                    ? velocity.GetCurrentVelocity().Normalized() * -1
                    : Vector2.Zero,
                TurretEffect.DynamicDirection.TargetObject => TargetObject != null
                    ? (TargetObject.GlobalPosition - GlobalPosition).Normalized()
                    : Vector2.Zero,
                _ => Vector2.Zero,
            };
        }

        private void RotateTurret(Vector2 targetPos)
        {
            if (_weaponOwner == null || _weaponOwner is not Node2D node)
            {
                return;
            }

            if (targetPos != Vector2.Zero)
            {
                // Get the angle to the target in global space
                float targetAngle = targetPos.Angle();

                // Set global rotation directly
                GlobalRotation = targetAngle;
            }
        }

        private void StartTimedRotation(float duration)
        {
            _rotationStarted = true;

            Tween tween = CreateTween();

            tween.TweenProperty(this, "global_rotation_degrees", 360, duration);
            tween.SetLoops(0);
        }
    }
}
