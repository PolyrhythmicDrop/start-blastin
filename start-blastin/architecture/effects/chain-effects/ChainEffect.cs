using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
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
                DebugLogger.LogMessage($"Chain condition met on {GetType().Name}!", true);
                EnableChainedEffects(target);
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            DisableChainedEffects(target);
        }

        protected virtual void EnableChainedEffects(GodotObject target)
        {
            foreach (Effect effect in _effects)
            {
                DebugLogger.LogMessage($"Enabling {effect}...", true);
                if (effect.Trigger == Trigger.Equip || effect.Trigger == Trigger.Chain)
                {
                    effect.Enable(target);
                }
                else if (effect.Target == TargetType.Chain)
                {
                    // This could be an issue if you can't change it back to the original target
                    effect.Target = Target;
                    effect.Enable();
                }
                else
                {
                    effect.Enable();
                }
            }
        }

        protected virtual void DisableChainedEffects(GodotObject target)
        {
            foreach (Effect effect in _effects)
            {
                if (effect.Trigger == Trigger.Chain || effect.Trigger == Trigger.Equip)
                {
                    DebugLogger.LogMessage($"Calling Disable() on {target} for {effect}...", true);
                    effect.Disable(target);
                }
                else
                {
                    effect.Disable();
                }
            }
        }
    }
}
