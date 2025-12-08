using System;
using System.Collections.Generic;
using Godot;
using Utility;

namespace Effects
{
    /// <summary>
    /// Effect that triggers attached effects when trigger conditions are met.
    /// Derived classes should override IsChainConditionMet() to activate chains.
    /// </summary>
    [GlobalClass]
    public abstract partial class ChainEffect : Effect
    {
        protected List<Effect> _effects = new();

        [ExportGroup("Effects")]
        [Export]
        public Godot.Collections.Array<Effect> Effects
        {
            get => [.. _effects];
            set => _effects = [.. value];
        }

        /// <summary>
        /// Recursively retrieve this effect and all nested effects, including triggered effects.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Effect> GetAllEffects()
        {
            yield return this;

            foreach (Effect effect in _effects)
            {
                foreach (Effect nestedEffect in effect.GetAllEffects())
                {
                    yield return nestedEffect;
                }
            }
        }

        protected abstract bool IsChainConditionMet(GodotObject target);

        protected override void OnApplyEffect(GodotObject target, EffectState effectState)
        {
            // // Get or create state of the effect
            // EffectState state = GetOrCreateEffectState(target);

            // // Update the state to track activation of the chain
            // state.Active = true;
            // if (_stacking)
            // {
            //     state.CurrentStacks++;
            // }

            // Check for chain trigger
            if (IsChainConditionMet(target))
            {
                TriggerChainedEffects(target);
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            // if (!_targetStates.ContainsKey(target))
            // {
            //     return;
            // }

            // EffectState state = _targetStates[target];

            // if (_stacking)
            // {
            //     state.CurrentStacks = Math.Max(0, state.CurrentStacks - 1);
            //     if (state.CurrentStacks == 0)
            //     {
            //         state.Active = false;
            //     }
            // }
            // else
            // {
            //     state.Active = false;
            // }
        }

        protected virtual void TriggerChainedEffects(GodotObject target)
        {
            foreach (Effect effect in _effects)
            {
                if (effect.Trigger == Trigger.Chain)
                {
                    DebugLogger.LogMessage($"Calling ApplyEffect() on {target}...", true);
                    effect.ApplyEffect(target);
                }
            }
        }
    }
}
