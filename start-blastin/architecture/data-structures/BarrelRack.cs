using System;
using System.Collections.Generic;
using Weapons;

namespace DataStructures
{
    public class BarrelRack : List<Barrel>
    {
        /// <summary>
        /// Activates or deactivates all barrels in a specific direction.
        /// </summary>
        /// <param name="dirs">The direction of the barrel to activate.</param>
        public void ToggleActivateBarrelsByDir(bool active, params Barrel.BarrelDirection[] dirs)
        {
            foreach (Barrel barrel in this)
            {
                foreach (Barrel.BarrelDirection direction in dirs)
                {
                    if (barrel.Direction == direction)
                    {
                        barrel.ToggleActive(active);
                    }
                }
            }
        }

        public void ToggleActivateBarrel(bool active, Barrel barrel)
        {
            Find(bar => bar.Equals(barrel)).ToggleActive(active);
        }

        public void ToggleActivateAllBarrels(bool active)
        {
            foreach (Barrel barrel in this)
            {
                if (barrel.Active != active)
                {
                    barrel.ToggleActive(active);
                }
            }
        }

        public IEnumerable<Barrel> GetBarrelsByDir(Barrel.BarrelDirection direction)
        {
            foreach (Barrel barrel in this)
            {
                if (barrel.Direction == direction)
                {
                    yield return barrel;
                }
            }
        }

        public IEnumerable<Barrel> GetBarrelsByActive(bool active)
        {
            foreach (Barrel barrel in this)
            {
                if (barrel.Active == active)
                {
                    yield return barrel;
                }
            }
        }
    }
}
