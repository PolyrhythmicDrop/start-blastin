using System;
using Godot;
using Projectiles;
using Weapons;

namespace Factories
{
    public class ProjectileFactory
    {
        public static Projectile CreateProjectile(WeaponNode weapon)
        {
            Projectile ammo = null;
            switch (weapon.Stats.ProjType)
            {
                default:
                case ProjectileType.Bullet:
                    ammo = GD.Load<PackedScene>(Bullet.ScenePath).Instantiate<Bullet>();
                    break;
            }
            ammo.SourceWeapon = weapon;
            return ammo;
        }
    }
}
