using Godot;
using NanoidDotNet;
using Projectiles;
using Weapons;

namespace Factories
{
    /// <summary>
    /// Factory for creating new projectiles.
    /// </summary>
    public static class ProjectileFactory
    {
        // Cached scenes
        private static PackedScene _bulletScene = GD.Load<PackedScene>(Bullet.ScenePath);
        private static PackedScene _missileScene = GD.Load<PackedScene>(Missile.ScenePath);

        // Cached shaders
        private static ShaderMaterial _bulletPalette = ResourceLoader.Load<ShaderMaterial>(
            "res://resources/materials/enemy-bullet-palette-swap.tres"
        );
        private static ShaderMaterial _missilePalette = ResourceLoader.Load<ShaderMaterial>(
            "uid://cfd51ihwk3ior"
        );

        /// <summary>
        /// Creates a new projectile appropriate for the passed weapon.
        /// </summary>
        /// <param name="weapon">The weapon to create the projectile for.</param>
        /// <returns>A new <see cref="Projectile"/></returns>
        public static Projectile CreateProjectile(WeaponNode weapon)
        {
            Projectile ammo;
            switch (weapon.Stats.ProjectileType)
            {
                default:
                case ProjectileType.Bullet:
                    ammo = _bulletScene.Instantiate<Bullet>();
                    break;
                case ProjectileType.Missile:
                    ammo = _missileScene.Instantiate<Missile>();
                    break;
            }
            ammo.SourceWeapon = weapon;

            SetProjectileShaderMaterial(ammo);
            SetProjectileCollisionLayers(ammo);
            ammo.Name = $"{ammo.GetType()}-{Nanoid.Generate(size: 8)}";
            return ammo;
        }

        private static void SetProjectileCollisionLayers(Projectile projectile)
        {
            if (projectile.SourceWeapon.EnemyOwned)
            {
                // Set the collision layer to 5 (Projectiles-Enemy).
                projectile.SetCollisionLayerValue(5, true);
                // Set the mask so the projectile does not hit other enemy projectiles.
                projectile.SetCollisionMaskValue(5, false);
                // Set the mask so that the projectile does not hit fellow enemies.
                projectile.SetCollisionMaskValue(3, false);
                // Set the mask so the projectile hits player projectiles.
                projectile.SetCollisionMaskValue(4, true);
            }
            else
            {
                // Set the collision layer to 4 (Projectiles-Player).
                projectile.SetCollisionLayerValue(4, true);
                // Set the mask so the projectile hits enemy projectiles.
                projectile.SetCollisionMaskValue(5, true);
                // Set the mask so that the projectile hits enemies.
                projectile.SetCollisionMaskValue(3, true);
                // Set the mask so the projectile does not hit other player projectiles.
                projectile.SetCollisionMaskValue(4, false);
            }
        }

        private static void SetProjectileShaderMaterial(Projectile projectile)
        {
            // TODO: Add different palette swaps for different types of projectiles
            if (projectile.SourceWeapon.EnemyOwned)
            {
                projectile.Material = projectile switch
                {
                    Bullet => _bulletPalette,
                    Missile => _missilePalette,
                    _ => _bulletPalette,
                };
            }
        }
    }
}
