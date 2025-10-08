using System;
using Godot;
using Projectiles;

namespace Factories
{
    public class ProjectileFactory
    {
        public static Projectile CreateProjectile(ProjectileType projectileType)
        {
            Projectile ammo = null;
            switch (projectileType)
            {
                default:
                case ProjectileType.Bullet:
                    ammo = GD.Load<PackedScene>(Bullet.ScenePath).Instantiate<Bullet>();
                    break;
            }
            return ammo;
        }
    }
}
