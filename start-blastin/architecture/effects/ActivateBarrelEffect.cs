using System;
using System.ComponentModel;
using DataStructures;
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
        [Export]
        public Barrel.BarrelDirection Direction { get; set; } = Barrel.BarrelDirection.North;

        [Export]
        public Operation Operation { get; set; } = Operation.Add;

        protected override void OnApplyEffect(GodotObject target, EffectState effectState)
        {
            if (target is not IWeaponOwner weaponOwner)
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
                        // Activate the barrel if an inactive with this direction is found on the target.
                        barrels.ToggleActivateBarrel(true, Direction);
                        break;
                    }

                    case Operation.Remove:
                    {
                        barrels.ToggleActivateBarrel(false, Direction);
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

            BarrelRack barrels = weaponOwner.Weapon?.Barrels;

            switch (this.Operation)
            {
                case Operation.Add:
                {
                    if (
                        barrels.Find(bar =>
                            (bar.Direction == this.Direction) && (bar.Active == true)
                        ) != null
                    )
                    {
                        // Deactivate the barrel in this direction.
                        barrels.ToggleActivateBarrel(false, Direction);
                    }
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
                        barrels.ToggleActivateBarrel(true, Direction);
                    }
                    break;
            }
        }
    }
}
