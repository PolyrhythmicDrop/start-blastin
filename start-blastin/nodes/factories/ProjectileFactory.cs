using System;
using Godot;
using Projectiles;
using Weapons;

namespace Factories
{
    public class ProjectileFactory
    {
        public static Projectile CreateProjectile(WeaponStats weaponStats)
        {
            Projectile ammo = null;
            switch (weaponStats.ProjType)
            {
                default:
                case ProjectileType.Bullet:
                    ammo = GD.Load<PackedScene>(Bullet.ScenePath).Instantiate<Bullet>();
                    break;
            }
            ammo.Speed = weaponStats.ProjSpeed;
            return ammo;
        }
    }
}
