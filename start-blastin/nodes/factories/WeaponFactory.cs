using System;
using Godot;
using Interfaces;
using WaveManagement;
using Weapons;

namespace Factories
{
    public class WeaponFactory
    {
        /// <summary>
        /// Instantiates and builds a weapon based on a passed resource.
        /// </summary>
        /// <param name="weaponResource">The resource to create the weapon from. Resource contains the weapon's scene path and stats.</param>
        /// <param name="enemyWeapon">True if the weapon belongs to an enemy. False if it belongs to the player. Used to apply the correct shader material to projectiles.</param>
        /// <param name="enemyScaler">Modifies the created weapon based on a wave configuration.</param>
        /// <returns>A built <see cref="WeaponNode"/> with its stats set by the passed <see cref="WeaponResource"/> and (optionally) <see cref="EnemyScaler"/>.</returns>
        public static WeaponNode CreateWeapon(
            WeaponResource weaponResource,
            bool enemyWeapon,
            EnemyScaler enemyScaler = null,
            IVelocityProvider velocityProvider = null
        )
        {
            try
            {
                GodotObject builtWeapon;

                if (weaponResource.ScenePath == null || weaponResource.ScenePath == "")
                {
                    GD.PrintErr("Scene path is empty! Skipping weapon creation...");
                    return null;
                }
                else
                {
                    builtWeapon = GD.Load<PackedScene>(weaponResource.ScenePath).Instantiate();
                }

                if (builtWeapon is WeaponNode weaponNode)
                {
                    weaponNode.EnemyOwned = enemyWeapon;
                    WeaponResource newResource = (WeaponResource)
                        weaponResource.DuplicateDeep(Resource.DeepDuplicateMode.Internal);
                    // If weapon is for an enemy and we have a wave config, apply the wave configuration to the weapon.
                    WeaponStats weaponStats = newResource.Stats;
                    // if (enemyWeapon && enemyScaler != null)
                    // {
                    //     ApplyWaveConfigToWeaponStats(weaponStats, enemyScaler);
                    // }
                    weaponNode.InitializeStats(weaponStats);
                    if (velocityProvider != null)
                    {
                        weaponNode.VelocityProvider = velocityProvider;
                    }
                    return weaponNode;
                }
                else
                {
                    throw new ArgumentException(
                        "Weapon resource either does not have a scene path or could not be instantiated!",
                        paramName: nameof(weaponResource)
                    );
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Applies a wave configuration to a weapon, typically for an enemy's weapon.
        /// </summary>
        /// <param name="stats">The original WeaponStats resource to modify.</param>
        /// <param name="config">The wave configuration that modifies the <paramref name="stats"/>.</param>
        private static void ApplyWaveConfigToWeaponStats(WeaponStats stats, EnemyScaler config)
        {
            stats.FireRate += config.FireRateModifier * stats.FireRate;
            stats.Damage += config.WeaponDamageModifier * stats.Damage;
        }
    }
}
