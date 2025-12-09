using System;
using Entities;
using Godot;
using Interfaces;
using Weapons;

namespace Effects
{
    /// <summary>
    /// Fires a shot in a static or dynamic direction.
    /// </summary>
    [GlobalClass]
    public partial class ShotEffect : Effect
    {
        protected class BarrelEffectState : EffectState
        {
            internal Barrel _barrel;

            internal BarrelEffectState(Effect parent)
                : base(parent) { }
        }

        private PackedScene _barrelScene = GD.Load<PackedScene>("uid://bajml0u2freln");

        [Export]
        public Barrel.BarrelDirection Direction { get; set; }

        /// <summary>
        /// Creates a new BarrelEffectState.
        /// </summary>
        /// <returns>A <see cref="BarrelEffectState"/> with the ShotEffect as the parent. </returns>
        protected override EffectState CreateEffectState()
        {
            return new BarrelEffectState(this);
        }

        /// <summary>
        /// Shoots a projectile.
        /// </summary>
        /// <param name="target">The object that fires the projectile.</param>
        /// <param name="state">The state of the effect.</param>
        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (state is not BarrelEffectState barrelState)
            {
                return;
            }

            if (target is not IWeaponOwner weaponOwner)
            {
                return;
            }

            if (barrelState._barrel == null)
            {
                // Create a new barrel and add it to the state and the target.
                barrelState._barrel = new Barrel(Direction);
                weaponOwner.Weapon.AddChild(barrelState._barrel);
                // Adjust the rotation if the target is a Player, since they're rotated -90 degrees always
                if (weaponOwner is Player)
                {
                    barrelState._barrel.GlobalRotationDegrees += 90;
                }

                barrelState._barrel.ToggleActive(true);
            }

            if (!_stacking)
            {
                // Fire a round from the barrel
                weaponOwner.Weapon.FireSingleBarrel(barrelState._barrel);
                // Clear the stack so you can fire it again next time the trigger is met
                barrelState.Active = false;
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state) { }
    }
}
