using System;
using System.ComponentModel;
using System.Linq;
using DataStructures;
using Factories;
using Godot;
using Interfaces;
using Utility;
using Weapons;

namespace Effects
{
    /// <summary>
    /// Activates or deactivates a weapon barrel on an entity.
    /// </summary>
    /// <remarks>
    /// The entity to barrel-ify must have barrels pre-configured in its scene.
    /// To add extra barrels to a weapon or to fire extra shots from the same barrel, use other effects.
    /// </remarks>
    [GlobalClass]
    public partial class ActivateBarrelEffect : Effect
    {
        protected class BarrelEffectState : EffectState
        {
            internal BarrelRack _barrelRack;

            internal BarrelEffectState(Effect parent)
                : base(parent) { }

            public override void CleanUpState(GodotObject target)
            {
                foreach (Barrel barrel in _barrelRack)
                {
                    if (barrel != null && IsInstanceValid(barrel))
                    {
                        barrel.ToggleActive(barrel.DefaultActive);
                        if (target is IWeaponOwner weaponOwner)
                        {
                            // Remove the barrel from the BarrelRack, scene tree, and memory if it's not one of the default Barrels.
                            if (weaponOwner.Weapon.IsAncestorOf(barrel) && !barrel.Base)
                            {
                                weaponOwner.Weapon.Barrels.Remove(barrel);
                                weaponOwner.Weapon.RemoveChild(barrel);
                                barrel.QueueFree();
                            }
                        }
                    }
                }
                _barrelRack.Clear();
            }
        }

        [Export]
        public Barrel.BarrelDirection Direction { get; set; } = Barrel.BarrelDirection.North;

        [Export]
        public Operation Operation { get; set; } = Operation.Add;

        /// <summary>
        /// Creates a new BarrelEffectState.
        /// </summary>
        /// <returns>A <see cref="BarrelEffectState"/> with the ShotEffect as the parent. </returns>
        protected override EffectState CreateEffectState()
        {
            return new BarrelEffectState(this);
        }

        protected override void OnApplyEffect(GodotObject target, EffectState effectState)
        {
            if (target is not IWeaponOwner weaponOwner)
            {
                return;
            }

            if (effectState is not BarrelEffectState barrelState)
            {
                return;
            }

            // Save the weapon owner's barrel rack as a variable
            BarrelRack barrels = weaponOwner.Weapon?.Barrels;

            try
            {
                switch (this.Operation)
                {
                    // Add a new barrel in the Effect's direction
                    case Operation.Add:
                    {
                        // If the state does not have a barrel rack, initialize it.
                        if (barrelState._barrelRack == null)
                        {
                            barrelState._barrelRack = new();
                        }

                        // If the state has a barrel rack with an inactive barrel in it, simply activate the inactive barrel.
                        if (barrelState._barrelRack != null && barrelState._barrelRack.Count > 0)
                        {
                            Barrel inactiveBarrel = barrelState
                                ._barrelRack.GetBarrelsByActive(false)
                                .FirstOrDefault();
                            // If you find an inactive barrel, make it active.
                            if (inactiveBarrel != null)
                            {
                                inactiveBarrel.ToggleActive(true);
                                return;
                            }
                        }
                        // If there are no inactive barrels in the existing rack, hunt for an inactive barrel in the weaponOwner's barrel rack.
                        else
                        {
                            // Get the first inactive barrel in the weaponOwner's barrel rack facing the correct direction
                            foreach (Barrel barrel in barrels.GetBarrelsByDir(Direction))
                            {
                                if (!barrel.Active)
                                {
                                    // Add the barrel to the state's barrel rack, activate it, and return.
                                    barrelState._barrelRack.Add(barrel);
                                    barrel.ToggleActive(true);
                                    return;
                                }
                            }
                        }
                        // If no inactive barrels are found in either barrel rack, create a new barrel, add it to the weaponOwner's rack, and activate it.
                        // All this is done with the CreateBarrel() call.
                        Barrel newBarrel = WeaponFactory.CreateBarrel<Barrel>(
                            weaponOwner,
                            Direction,
                            true,
                            true
                        );
                        // Add the new barrel to the state's weapon rack.
                        barrelState._barrelRack.Add(newBarrel);
                        break;
                    }
                    // Remove or deactivate a barrel in the effect's direction
                    case Operation.Remove:
                    {
                        // If the state does not have a barrel rack, initialize it.
                        if (barrelState._barrelRack == null)
                        {
                            barrelState._barrelRack = new();
                        }

                        // If the state has a barrel rack with an active barrel in it, simply de-activate the inactive barrel.
                        if (barrelState._barrelRack.Count > 0)
                        {
                            Barrel activeBarrel = barrelState
                                ._barrelRack.GetBarrelsByActive(true)
                                .FirstOrDefault();
                            // If you find an active barrel, make it inactive.
                            if (activeBarrel != null)
                            {
                                activeBarrel.ToggleActive(false);
                                return;
                            }
                        }

                        // If we didn't find an active barrel in the current state, find an active barrel in the current direction in the owner's rack instead
                        foreach (Barrel barrel in barrels.GetBarrelsByDir(Direction))
                        {
                            if (barrel.Active)
                            {
                                // Add the barrel to the state's rack for tracking
                                barrelState._barrelRack.Add(barrel);
                                // Toggle it active.
                                barrel.ToggleActive(false);
                                return;
                            }
                        }
                        // If we didn't find anything active in either rack, then there are probably no active barrels in this direction. Therefore, there's nothing to do!
                        break;
                    }
                    case Operation.Multiply:
                    {
                        throw new InvalidEnumArgumentException(
                            $"Cannot use {this.Operation} in BarrelEffect!"
                        );
                    }
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            if (target is not IWeaponOwner weaponOwner)
            {
                return;
            }

            if (effectState is not BarrelEffectState barrelState)
            {
                return;
            }

            switch (this.Operation)
            {
                case Operation.Add:
                {
                    // Find the first active barrel in the state's barrel rack and deactivate it.
                    Barrel activeBarrel = barrelState
                        ._barrelRack?.GetBarrelsByActive(true)
                        .FirstOrDefault();
                    if (activeBarrel != null)
                    {
                        activeBarrel.ToggleActive(false);
                    }
                    break;
                }
                case Operation.Remove:

                    // If the state is active, make barrels inactive until the state is no longer active.
                    if (barrelState.Active == true)
                    {
                        // Find the first inactive barrel in the state's barrel rack and re-activate it.
                        Barrel inactiveBarrel = barrelState
                            ._barrelRack?.GetBarrelsByActive(false)
                            .FirstOrDefault();
                        if (inactiveBarrel != null)
                        {
                            inactiveBarrel.ToggleActive(true);
                        }
                    }

                    break;
            }
        }
    }
}
