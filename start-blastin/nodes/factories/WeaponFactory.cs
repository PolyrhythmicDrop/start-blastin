using System;
using Godot;
using Weapons;

namespace Factories
{
    public class WeaponFactory
    {
        public static WeaponNode CreateWeapon(WeaponResource weaponResource)
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
                    weaponNode.InitializeStats(weaponResource.Stats);
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
