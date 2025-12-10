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
            // Check for chain trigger
            if (IsChainConditionMet(target))
            {
                TriggerChainedEffects(target);
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            foreach (Effect effect in _effects)
            {
                if (effect.Trigger == Trigger.Chain)
                {
                    effect.RemoveAllEffectsFromTarget(target);
                }
            }
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
