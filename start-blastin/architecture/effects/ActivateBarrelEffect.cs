using System;
using System.ComponentModel;
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
            internal Barrel _barrel;

            internal BarrelEffectState(Effect parent)
                : base(parent) { }

            public override void CleanUpState(GodotObject target)
            {
                DebugLogger.LogMessage($"Attemping to clean up {_barrel.Name}...", true);

                if (_barrel != null && !_barrel.IsQueuedForDeletion())
                {
                    DebugLogger.LogMessage($"{_barrel.Name} is being cleaned up!", true);
                    if (target is IWeaponOwner weaponOwner)
                    {
                        // Remove the barrel from the BarrelRack, scene tree, and memory if it's not one of the default Barrels.
                        if (weaponOwner.Weapon.IsAncestorOf(_barrel) && !_barrel.Base)
                        {
                            weaponOwner.Weapon.Barrels.Remove(_barrel);
                            weaponOwner.Weapon.RemoveChild(_barrel);
                            _barrel.QueueFree();
                        }
                        else
                        {
                            _barrel = null;
                        }
                    }
                }
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

            BarrelRack barrels = weaponOwner.Weapon?.Barrels;

            try
            {
                switch (this.Operation)
                {
                    case Operation.Add:
                    {
                        // Activate the barrel if an inactive barrel with this direction is found on the target.
                        // barrels.ToggleActivateBarrel(true, Direction);

                        // If there's already a barrel as part of the state, activate it.
                        if (barrelState._barrel != null)
                        {
                            barrelState._barrel.ToggleActive(true);
                        }
                        else
                        {
                            // Get the first inactive barrel in the barrel rack facing the correct direction and activate it
                            foreach (Barrel barrel in barrels.GetBarrelsByDir(Direction))
                            {
                                if (!barrel.Active)
                                {
                                    barrelState._barrel = barrel;
                                    barrel.ToggleActive(true);
                                    return;
                                }
                            }

                            // If no inactive barrels are found, create a new one and activate it.
                            Barrel newBarrel = WeaponFactory.CreateBarrel(
                                weaponOwner,
                                Direction,
                                true,
                                true
                            );
                            // Add the new barrel to the state
                            barrelState._barrel = newBarrel;
                        }
                        break;
                    }

                    case Operation.Remove:
                    {
                        barrels.ToggleActivateBarrelsByDir(false, Direction);
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

            BarrelRack barrels = weaponOwner.Weapon?.Barrels;

            switch (this.Operation)
            {
                case Operation.Add:
                {
                    // if (
                    //     barrels.Find(bar =>
                    //         (bar.Direction == this.Direction) && (bar.Active == true)
                    //     ) != null
                    // )
                    // {
                    //     // Deactivate the barrel in this direction.
                    //     barrels.ToggleActivateBarrelsByDir(false, Direction);
                    // }

                    // Get the barrel from the state and deactivate it.
                    barrelState._barrel?.ToggleActive(false);
                    break;
                }
                case Operation.Remove:
                    if (
                        barrels.Find(bar =>
                            (bar.Direction == this.Direction) && (bar.Active == false)
                        ) != null
                    )
                    {
                        // Activate the previously-deactivated barrel.
                        barrels.ToggleActivateBarrelsByDir(true, Direction);
                    }
                    break;
            }
        }
    }
}
