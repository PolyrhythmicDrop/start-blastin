using Entities;
using Factories;
using Godot;
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
                    false,
                    velocityProvider: _player
                );
                GD.Print(
                    $"Weapon equipped! {_equippedWeapon}\nStats: {_equippedWeapon.Stats.FireRate} | {_equippedWeapon.Stats.Damage} | {_equippedWeapon.Stats.ProjectileType} | {_equippedWeapon.Stats.ProjectileSpeed}"
                );

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
                fireTimer.Start();
            }
        }

        public void StopWeapon()
        {
            _equippedWeapon.FireTimer.Stop();
        }
    }
}
