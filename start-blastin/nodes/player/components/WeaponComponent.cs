using Entities;
using Factories;
using Godot;
using Weapons;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class WeaponComponent : Node
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
                _equippedWeapon = WeaponFactory.CreateWeapon(weaponResource);
                GD.Print(
                    $"Weapon equipped! {_equippedWeapon}\nStats: {_equippedWeapon.Stats.FireRate} | {_equippedWeapon.Stats.Damage} | {_equippedWeapon.Stats.ProjType} | {_equippedWeapon.Stats.ProjSpeed}"
                );

                AddChild(_equippedWeapon);
            }
        }
    }
}
