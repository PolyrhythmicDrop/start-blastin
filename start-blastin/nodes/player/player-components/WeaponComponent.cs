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
        private WeaponNode _equippedWeapon;

        [Export]
        public WeaponResource InitWeaponResource
        {
            get => _initWeaponResource;
            set => _initWeaponResource = value;
        }

        public WeaponNode Weapon => _equippedWeapon;

        public void Initialize(Player player)
        {
            _player = player;
            EquipWeapon(InitWeaponResource);
        }

        public void EquipWeapon(WeaponResource weaponResource)
        {
            if (_equippedWeapon != null)
            {
                RemoveChild(_equippedWeapon);
            }
            else
            {
                _equippedWeapon = WeaponFactory.CreateWeapon(
                    weaponResource,
                    velocityProvider: _player,
                    owner: _player
                );
                // Initialize stats
                _equippedWeapon.Stats.FireRate = _player.FireRate;
                _equippedWeapon.Stats.Damage = _player.Damage;
                _equippedWeapon.Stats.ProjectileSpeed = _player.ProjectileSpeed;

                AddChild(_equippedWeapon);
                ConnectFireTimerSignals();
            }
        }

        private void ConnectFireTimerSignals()
        {
            if (_equippedWeapon != null)
            {
                _equippedWeapon.FireTimer.Timeout += _equippedWeapon.Fire;
            }
        }

        public void FireWeapon()
        {
            Timer fireTimer = _equippedWeapon.FireTimer;

            if (fireTimer.IsStopped())
            {
                _equippedWeapon.Fire();
                fireTimer.Start(_equippedWeapon.Stats.FireRate);
            }
        }

        public void StopWeapon()
        {
            _equippedWeapon.FireTimer.Stop();
        }

        public void SetWeaponProjectile(ProjectileType type)
        {
            _equippedWeapon.Stats.ProjectileType = type;
            // Clear the projectile pool and reset it with the new projectile type.
            // _equippedWeapon.InitializeProjectilePool();
            _equippedWeapon.ResetProjectilePool();
        }
    }
}
