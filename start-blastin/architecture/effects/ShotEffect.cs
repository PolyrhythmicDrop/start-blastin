using Factories;
using Godot;
using Interfaces;
using Weapons;

namespace Effects
{
    /// <summary>
    /// Fires one or more shots in a static or dynamic direction.
    /// </summary>
    [GlobalClass]
    public partial class ShotEffect : Effect
    {
        protected class ShotEffectState : EffectState
        {
            internal Barrel _barrel;

            internal ShotEffectState(Effect parent)
                : base(parent) { }
        }

        [Export]
        public Barrel.BarrelDirection Direction { get; set; }

        /// <summary>
        /// Creates a new BarrelEffectState.
        /// </summary>
        /// <returns>A <see cref="ShotEffectState"/> with the ShotEffect as the parent. </returns>
        protected override EffectState CreateEffectState()
        {
            return new ShotEffectState(this);
        }

        /// <summary>
        /// Shoots a projectile.
        /// </summary>
        /// <param name="target">The object that fires the projectile.</param>
        /// <param name="state">The state of the effect.</param>
        protected override async void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (state is not ShotEffectState barrelState)
            {
                return;
            }

            if (target is not IWeaponOwner weaponOwner)
            {
                return;
            }

            if (barrelState._barrel == null)
            {
                AddBarrel(weaponOwner, barrelState);
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
            if (state is not ShotEffectState barrelState)
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

        private void AddBarrel(IWeaponOwner weaponOwner, ShotEffectState barrelState)
        {
            // Create a new barrel and add it to the state and the target.
            barrelState._barrel = WeaponFactory.CreateBarrel<Barrel>(
                weaponOwner,
                Direction,
                activate: true
            );
        }

        private void RemoveBarrel(IWeaponOwner weaponOwner, ShotEffectState barrelState)
        {
            barrelState._barrel.ToggleActive(false);
            weaponOwner.Weapon.RemoveChild(barrelState._barrel);
            barrelState._barrel.QueueFree();
        }
    }
}
