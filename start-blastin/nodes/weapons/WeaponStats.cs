using Godot;
using Projectiles;

namespace Weapons
{
    /// <summary>
    /// Data container for the different statistics of a weapon.
    /// </summary>
    [GlobalClass]
    public partial class WeaponStats : Resource
    {
        private ProjectileType _projType = ProjectileType.Bullet;
        private float _damage = 1;
        private float _fireRate = 0.5f;
        private float _projSpeed = 200;

        /// <summary>
        /// The type of projectile fired by the eweapon.
        /// </summary>
        [Export]
        public ProjectileType ProjectileType
        {
            get => _projType;
            set => _projType = value;
        }

        /// <summary>
        /// The damage done by this weapon.
        /// </summary>
        [Export]
        public float Damage
        {
            get => _damage;
            set => _damage = value;
        }

        /// <summary>
        /// Rate of fire for the weapon, used in the FireTimer.
        /// Lower values mean a faster fire rate.
        /// </summary>
        [Export]
        public float FireRate
        {
            get => _fireRate;
            set => _fireRate = value;
        }

        /// <summary>
        /// The base speed of a projectile coming out of this weapon.
        /// </summary>
        /// <remarks>
        /// Projectile speed is augmented by the firing object's speed.
        /// </remarks>
        [Export]
        public float ProjectileSpeed
        {
            get => _projSpeed;
            set => _projSpeed = value;
        }
    }
}
