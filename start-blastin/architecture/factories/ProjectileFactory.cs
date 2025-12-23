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

            SetProjectileShaderMaterial(ammo, ammo.SourceWeapon.EnemyOwned);
            ammo.SetProjectileCollisionLayers(ammo.SourceWeapon.EnemyOwned);
            ammo.Name = $"{ammo.GetType()}-{Nanoid.Generate(size: 8)}";
            return ammo;
        }

        // private static void SetProjectileCollisionLayers(Projectile projectile, bool enemy)
        // {
        //     if (enemy)
        //     {
        //         // Set the collision layer 5 (Projectiles-Enemy) to true.
        //         projectile.SetCollisionLayerValue(5, true);
        //         // Set collision layer 4 (Projectiles-Player) to false.
        //         projectile.SetCollisionLayerValue(4, false);
        //         // Set the mask so the projectile hits players.
        //         projectile.SetCollisionMaskValue(1, true);
        //         // Set the mask so that the projectile does not hit fellow enemies.
        //         projectile.SetCollisionMaskValue(3, false);
        //         // Set the mask so the projectile does not hit other enemy projectiles.
        //         projectile.SetCollisionMaskValue(5, false);
        //         // Set the mask so the projectile hits player projectiles.
        //         projectile.SetCollisionMaskValue(4, true);
        //     }
        //     else
        //     {
        //         // Set the collision layer to 4 (Projectiles-Player).
        //         projectile.SetCollisionLayerValue(4, true);
        //         // Set collision layer 5 (Projectiles-Enemy) to false.
        //         projectile.SetCollisionLayerValue(5, false);
        //         // Set the mask so the projectile does not hit players.
        //         projectile.SetCollisionMaskValue(1, false);
        //         // Set the mask so that the projectile hits enemies.
        //         projectile.SetCollisionMaskValue(3, true);
        //         // Set the mask so the projectile does not hit other player projectiles.
        //         projectile.SetCollisionMaskValue(4, false);
        //         // Set the mask so the projectile hits enemy projectiles.
        //         projectile.SetCollisionMaskValue(5, true);
        //     }
        // }

        private static void SetProjectileShaderMaterial(Projectile projectile, bool enemy)
        {
            // TODO: Add different palette swaps for different types of projectiles
            if (enemy)
            {
                projectile.Material = projectile switch
                {
                    Bullet => _bulletPalette,
                    Missile => _missilePalette,
                    _ => _bulletPalette,
                };
            }
            else
            {
                projectile.Material = null;
            }
        }

        // /// <summary>
        // /// Converts the projectile to a new owner, either the player or an enemy.
        // /// Affects the shader material and the collision layers.
        // /// </summary>
        // /// <param name="projectile">The projectile to convert.</param>
        // /// <param name="enemy">True if you want to turn the projectile into a enemy projectile, false to turn it into an player projectile.</param>
        // public static void ConvertProjectileOwner(Projectile projectile, bool enemy)
        // {
        //     // Set the collision layers for the projectile and its RayCast
        //     SetProjectileCollisionLayers(projectile, enemy);
        //     projectile.SetRayMask(enemy);

        //     // Set the shader material
        //     SetProjectileShaderMaterial(projectile, enemy);

        //     // Remove the projectile from the enemy pool so it doesn't get re-used.
        //     projectile.SourceWeapon.Pool.Remove(projectile);

        //     // If the projectile is a Missile, remove the current target
        //     if (projectile is Missile missile)
        //     {
        //         missile.RemoveCurrentTarget();
        //     }
        // }
    }
}
