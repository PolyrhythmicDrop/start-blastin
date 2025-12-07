using System.Collections.Generic;
using DataStructures;
using Entities;
using Factories;
using Godot;
using Projectiles;
using Utility;
using Weapons;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class WeaponComponent : Node2D
    {
        private Player _player;
        private WeaponResource _initWeaponResource;
        private WeaponNode _weapon;

        public BarrelRack Barrels => _weapon?.Barrels;

        [Export]
        public WeaponResource InitWeaponResource
        {
            get => _initWeaponResource;
            set => _initWeaponResource = value;
        }

        public WeaponNode Weapon => _weapon;

        public void Initialize(Player player)
        {
            _player = player;
            EquipWeapon(InitWeaponResource);
        }

        public void EquipWeapon(WeaponResource weaponResource)
        {
            if (_weapon != null)
            {
                RemoveChild(_weapon);
            }
            else
            {
                _weapon = WeaponFactory.CreateWeapon(
                    weaponResource,
                    velocityProvider: _player,
                    owner: _player
                );
                // Initialize stats
                _weapon.Stats.FireRate = _player.FireRate;
                _weapon.Stats.Damage = _player.Damage;
                _weapon.Stats.ProjectileSpeed = _player.ProjectileSpeed;

                AddChild(_weapon);
                ConnectFireTimerSignals();
            }
        }

        private void ConnectFireTimerSignals()
        {
            if (_weapon != null)
            {
                _weapon.FireTimer.Timeout += _weapon.Fire;
            }
        }

        public void FireWeapon()
        {
            Timer fireTimer = _weapon.FireTimer;

            if (fireTimer.IsStopped())
            {
                _weapon.Fire();
                fireTimer.Start(_weapon.Stats.FireRate);
            }
        }

        public void StopWeapon()
        {
            _weapon.FireTimer.Stop();
        }

        public void SetWeaponProjectile(ProjectileType type)
        {
            _weapon.Stats.ProjectileType = type;
            // Clear the projectile pool and reset it with the new projectile type.
            _weapon.ResetProjectilePool();
        }
    }
}
