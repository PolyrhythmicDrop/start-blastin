using System;
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
        private IWeaponOwner _weaponOwner;

        public Vector2 TargetPosition { get; set; }

        public TurretBarrel(BarrelDirection direction)
            : base(direction) { }

        public void SetDynamicDirection(TurretEffect.DynamicDirection dir)
        {
            _dynamicDir = dir;
            // if (_dynamicDir != TurretEffect.DynamicDirection.None)
            // {
            //     GlobalRotation = 0;
            // }
        }

        public void SetWeaponOwner(IWeaponOwner owner)
        {
            _weaponOwner = owner;
        }

        public override void _Process(double delta)
        {
            if (_dynamicDir == TurretEffect.DynamicDirection.None || Active == false)
            {
                return;
            }

            SetTargetPosition();
            RotateTurret(TargetPosition);
        }

        private void SetTargetPosition()
        {
            TargetPosition = _dynamicDir switch
            {
                TurretEffect.DynamicDirection.Movement => _weaponOwner is IVelocityProvider velocity
                    ? velocity.GetCurrentVelocity()
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
    }
}
