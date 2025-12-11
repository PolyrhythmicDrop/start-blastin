using System;
using Enemies;
using Godot;
using Interfaces;
using NanoidDotNet;
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
                builtWeapon.Name = $"{node?.Name}-{builtWeapon.GetType().Name}";
            }

            return builtWeapon;
        }

        /// <summary>
        /// Creates a new barrel.
        /// </summary>
        /// <param name="weaponOwner">The owner of the barrel. The barrel is added as a child of the <paramref name="weaponOwner"/> if one is passed.</param>
        /// <param name="direction">The direction the barrel will shoot in, relative to the parent.</param>
        /// <param name="addToRack">Whether to add the barrel to the <paramref name="weaponOwner"/>'s rack. Only works if <paramref name="weaponOwner"/> is not false.</param>
        /// <param name="activate">Whether to activate the barrel before returning.</param>
        /// <returns>A new barrel object.</returns>
        public static Barrel CreateBarrel(
            IWeaponOwner weaponOwner = null,
            Barrel.BarrelDirection direction = Barrel.BarrelDirection.North,
            bool addToRack = false,
            bool activate = false
        )
        {
            Barrel barrel = new(direction);

            if (weaponOwner != null)
            {
                weaponOwner.Weapon?.AddChild(barrel);
                if (addToRack)
                {
                    weaponOwner.Weapon?.Barrels.Add(barrel);
                }
            }

            barrel.ToggleActive(activate);
            string dirChar = direction switch
            {
                Barrel.BarrelDirection.East => "E",
                Barrel.BarrelDirection.North => "N",
                Barrel.BarrelDirection.Northeast => "NE",
                Barrel.BarrelDirection.Northwest => "NW",
                Barrel.BarrelDirection.South => "S",
                Barrel.BarrelDirection.Southwest => "SW",
                Barrel.BarrelDirection.West => "W",
                _ => "",
            };
            barrel.Name = $"Barrel{dirChar}-{Nanoid.Generate(size: 3)}";

            return barrel;
        }
    }
}
