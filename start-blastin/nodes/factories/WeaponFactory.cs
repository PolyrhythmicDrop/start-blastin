using System;
using Godot;
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
        /// <returns></returns>
        public static WeaponNode CreateWeapon(WeaponResource weaponResource, bool enemyWeapon)
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
                    weaponNode.InitializeStats(newResource.Stats);
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
    }
}
