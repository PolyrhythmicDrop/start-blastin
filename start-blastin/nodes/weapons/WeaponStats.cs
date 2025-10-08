using Godot;
using Projectiles;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponStats : Resource
    {
        private ProjectileType _projType = ProjectileType.Bullet;
        private float _damage = 1;
        private float _fireRate = 0.5f;
        private float _projSpeed = 200;

        [Export]
        public ProjectileType ProjType
        {
            get => _projType;
            set => _projType = value;
        }

        [Export]
        public float Damage
        {
            get => _damage;
            set => _damage = value;
        }

        [Export]
        public float FireRate
        {
            get => _fireRate;
            set => _fireRate = value;
        }

        [Export]
        public float ProjSpeed
        {
            get => _projSpeed;
            set => _projSpeed = value;
        }
    }
}
