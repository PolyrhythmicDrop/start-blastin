using System.Reflection;
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

            // If the weapon belongs to an enemy, apply the relevant shader to its projectiles.
            if (weapon.EnemyOwned)
            {
                ShaderMaterial shaderMaterial = ResourceLoader.Load<ShaderMaterial>(
                    "res://resources/materials/enemy-bullet-palette-swap.tres"
                );
                ammo.Material = shaderMaterial;
            }
            return ammo;
        }
    }
}
