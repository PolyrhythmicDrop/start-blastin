using System;
using System.Runtime.CompilerServices;
using Godot;
using Utility;

namespace Effects
{
    /// <summary>
    /// Chain effect that triggers its nested effects when its event count reaches a threshold.
    /// </summary>
    [GlobalClass]
    public partial class CountChainEffect : ChainEffect
    {
        protected class CountEffectState : EffectState
        {
            internal int _count = 0;

            internal bool _effectsEnabled = false;

            internal CountEffectState(Effect parent)
                : base(parent) { }
        }

        // public int _count = 0;

        [Export(PropertyHint.Range, "1,50,1,greater_than")]
        public int Threshold { get; set; } = 1;

        protected override EffectState CreateEffectState()
        {
            return new CountEffectState(this);
        }

        protected override bool IsChainConditionMet(GodotObject target)
        {
            if (_targetStates.TryGetValue(target, out EffectState state))
            {
                if (state is CountEffectState countState)
                {
                    return countState._count >= Threshold;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        protected override void OnApplyEffect(GodotObject target, EffectState effectState)
        {
            // Increment the count since we're applying the effect.
            if (effectState is not CountEffectState countState)
            {
                return;
            }

            countState._count++;
            DebugLogger.LogMessage($"Applying {GetType().Name}! Count: {countState._count}", true);

            // Check for the condition using the base method.
            // Moved this to UpdateEffectState override
            // base.OnApplyEffect(target, countState);
        }

        /// <summary>
        /// Update the effect state to only be active once the threshold has been met, and to ignore stacking.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="state"></param>
        /// <param name="postApplication"></param>
        protected override void UpdateEffectState(
            GodotObject target,
            EffectState state,
            bool postApplication
        )
        {
            if (state is not CountEffectState countState)
            {
                return;
            }

            DebugLogger.LogMessage(
                $"Updating effect state on {GetType().Name} for {target} & {countState}",
                true
            );

            if (postApplication)
            {
                // Check for chain trigger
                if (IsChainConditionMet(target) && !countState._effectsEnabled)
                {
                    DebugLogger.LogMessage($"Chain condition met on {GetType().Name}!", true);
                    EnableChainedEffects(target);
                    state.Active = true;
                    countState._effectsEnabled = true;
                    if (_stacking)
                    {
                        state.CurrentStacks = 1;
                    }

                    if (_timed)
                    {
                        StartTimer(target, state);
                    }
                }
            }
            else
            {
                if (countState._effectsEnabled)
                {
                    if (_stacking)
                    {
                        state.CurrentStacks = 0;
                    }
                    else
                    {
                        state.Active = false;
                    }
                    countState._effectsEnabled = false;
                }
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            base.OnRemoveEffect(target, effectState);

            if (effectState is not CountEffectState countState)
            {
                return;
            }
            // Reset the count on effect removal
            ResetCount(target, countState);
            DebugLogger.LogMessage($"Removing {GetType().Name}! Count: {countState._count}", true);
        }

        protected override void EnableChainedEffects(GodotObject target)
        {
            base.EnableChainedEffects(target);
        }

        private void ResetCount(GodotObject target, CountEffectState countState = null)
        {
            if (countState != null)
            {
                countState._count = 0;
            }
            else if (_targetStates.TryGetValue(target, out EffectState state))
            {
                if (state is CountEffectState count)
                {
                    count._count = 0;
                }
            }
        }
    }
}
