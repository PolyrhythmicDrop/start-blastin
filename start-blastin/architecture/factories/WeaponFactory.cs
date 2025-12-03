using System;
using Enemies;
using Godot;
using Interfaces;
using Utility;
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
            IVelocityProvider velocityProvider = null,
            IWeaponOwner owner = null
        )
        {
            WeaponNode builtWeapon = new();

            builtWeapon.EnemyOwned = owner is EnemyNode ? true : false;
            if (owner != null)
            {
                builtWeapon.SetOwner(owner);
            }

            WeaponResource newResource = (WeaponResource)
                weaponResource.DuplicateDeep(Resource.DeepDuplicateMode.Internal);
            WeaponStats weaponStats = newResource.Stats;

            builtWeapon.InitializeStats(weaponStats);

            if (velocityProvider != null)
            {
                builtWeapon.VelocityProvider = velocityProvider;
            }

            if (owner is Node node)
            {
                DebugLogger.LogMessage($"Weapon created for {node.Name}. owner: {owner}");
                builtWeapon.Name = $"{node?.Name}-{builtWeapon.GetType().Name}";
            }

            return builtWeapon;
        }
    }
}
