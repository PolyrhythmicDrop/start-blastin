using DataStructures;
using Entities;
using Factories;
using Godot;
using Items;
using Projectiles;
using Weapons;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class WeaponComponent : Node2D
    {
        private Player _player;

        // private WeaponResource _initWeaponResource;
        private WeaponNode _weaponNode;

        private WeaponPlugin _weaponPlugin => _player?.Inventory?.WeaponPlugin;

        public BarrelRack Barrels => _weaponNode?.Barrels;

        // [Export]
        // public WeaponResource InitWeaponResource
        // {
        //     get => _initWeaponResource;
        //     set => _initWeaponResource = value;
        // }

        public WeaponNode Weapon => _weaponNode;

        public void Initialize(Player player)
        {
            _player = player;
            InitializeWeaponNode();
            // EquipWeapon(InitWeaponResource);
        }

        /// <summary>
        /// Builds the weapon node based on the initial weapon plugin.
        /// </summary>
        /// <remarks>
        /// The player's <see cref="InventoryComponent"/> must be initialized before you call this method!
        /// </remarks>
        public void InitializeWeaponNode()
        {
            // Create a new weapon resource based on the player's stats and the weapon plugin.
            WeaponStats weaponStats = new()
            {
                ProjectileType = _player.Inventory.WeaponPlugin.ProjectileType,
                Damage = _player.Damage,
                FireRate = _player.FireRate,
                ProjectileSpeed = _player.ProjectileSpeed,
            };

            // Create the weapon node using the stats.
            WeaponResource resource = new() { Stats = weaponStats };

            _weaponNode = WeaponFactory.CreateWeapon(
                resource,
                velocityProvider: _player,
                owner: _player
            );

            AddChild(_weaponNode);

            ConnectFireTimerSignals();

            // if (_weaponNode != null)
            // {
            //     RemoveChild(_weaponNode);
            // }
            // else
            // {
            //     _weaponNode = WeaponFactory.CreateWeapon(
            //         weaponResource,
            //         velocityProvider: _player,
            //         owner: _player
            //     );
            //     // Initialize stats
            //     _weaponNode.Stats.FireRate = _player.FireRate;
            //     _weaponNode.Stats.Damage = _player.Damage;
            //     _weaponNode.Stats.ProjectileSpeed = _player.ProjectileSpeed;

            //     AddChild(_weaponNode);
            //     ConnectFireTimerSignals();
            // }
        }

        private void ConnectFireTimerSignals()
        {
            if (_weaponNode != null)
            {
                _weaponNode.FireTimer.Timeout += _weaponNode.Fire;
                _weaponNode.FireTimer.Timeout += () =>
                {
                    _player.Audio.PlayFireSound();
                };
            }
        }

        public void FireWeapon()
        {
            Timer fireTimer = _weaponNode.FireTimer;

            if (fireTimer.IsStopped())
            {
                _weaponNode.Fire();
                _player.Audio.PlayFireSound();
                fireTimer.Start(_weaponNode.Stats.FireRate);
            }
        }

        public void StopWeapon()
        {
            _weaponNode.FireTimer.Stop();
        }

        public void SetWeaponProjectile(ProjectileType type)
        {
            _weaponNode.Stats.ProjectileType = type;
            // Clear the projectile pool and reset it with the new projectile type.
            _weaponNode.ResetProjectilePool();
        }
    }
}
