using System;
using System.Collections.Generic;
using System.Diagnostics;
using Utility;
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

        /// <summary>
        /// Activates a single barrel in the barrel rack.
        /// </summary>
        /// <param name="active">Activates the barrel if true, deactivates it if false.</param>
        /// <param name="barrel">The barrel to activate or deactivate.</param>
        public bool ToggleActivateBarrel(bool active, Barrel barrel)
        {
            try
            {
                if (Contains(barrel))
                {
                    barrel.ToggleActive(active);
                    return true;
                }
                else
                {
                    throw new ArgumentException(
                        $"Could not find {barrel} in {this}! Cannot activate barrel."
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return false;
            }
        }

        /// <summary>
        /// Toggles all barrels in this barrel rack on or off.
        /// </summary>
        /// <param name="active">Activates barrels if true, deactivates barrels if false.</param>
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

        /// <summary>
        /// Gets all barrels of a certain direction in this barrel rack.
        /// </summary>
        /// <param name="direction">The direction of the barrels to get.</param>
        /// <returns>Barrels whose orientation is in the passed direction.</returns>
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

        /// <summary>
        /// Gets all active or inactive barrels in this barrel rack.
        /// </summary>
        /// <param name="active">True to search for active barrels, false to search for inactive barrels.</param>
        /// <returns>An IEnumerable container of active or inactive barrels.</returns>
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
