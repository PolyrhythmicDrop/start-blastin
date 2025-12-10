using System;
using System.Threading.Tasks;
using Entities;
using Godot;
using Interfaces;
using Utility;
using Weapons;

namespace Effects
{
    /// <summary>
    /// Fires one or more shots in a static or dynamic direction.
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
        protected override async void OnApplyEffect(GodotObject target, EffectState state)
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
                CreateBarrel(weaponOwner, barrelState);
            }

            if (!_stacking || _stacking && _maxStacks < 0)
            {
                // Fire a single round from the barrel
                weaponOwner.Weapon.FireSingleBarrel(barrelState._barrel);
            }
            else if (_stacking && _maxStacks > 0)
            {
                // Fire the shot for the number of max stacks.
                for (int i = 0; i < _maxStacks; i++)
                {
                    weaponOwner.Weapon.FireSingleBarrel(barrelState._barrel);
                    // Add a slight delay between firings so the projectiles don't all spawn on top of each other.
                    await ToSignal(
                        weaponOwner.Weapon.GetTree().CreateTimer(0.1f),
                        SceneTreeTimer.SignalName.Timeout
                    );
                }
                // Reset CurrentStacks so we can keep firing without immediately returning.
                state.CurrentStacks = 1;
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            if (state is not BarrelEffectState barrelState)
            {
                return;
            }

            if (target is not IWeaponOwner weaponOwner)
            {
                return;
            }

            if (barrelState._barrel != null && !barrelState._barrel.IsQueuedForDeletion())
            {
                RemoveBarrel(weaponOwner, barrelState);
            }
        }

        private void CreateBarrel(IWeaponOwner weaponOwner, BarrelEffectState barrelState)
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

        private void RemoveBarrel(IWeaponOwner weaponOwner, BarrelEffectState barrelState)
        {
            barrelState._barrel.ToggleActive(false);
            weaponOwner.Weapon.RemoveChild(barrelState._barrel);
            barrelState._barrel.QueueFree();
        }
    }
}
