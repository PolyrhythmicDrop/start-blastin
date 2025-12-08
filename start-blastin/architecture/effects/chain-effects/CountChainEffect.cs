using System;
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
        public int _count = 0;

        [Export(PropertyHint.Range, "1,50,1,greater_than")]
        public int Threshold { get; set; } = 1;

        protected override bool IsChainConditionMet(GodotObject target)
        {
            return _count >= Threshold;
        }

        protected override void OnApplyEffect(GodotObject target, EffectState effectState)
        {
            // Increment the count since we're applying the effect.
            _count++;
            // Check for the condition using the base method.
            base.OnApplyEffect(target, effectState);
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            base.OnRemoveEffect(target, effectState);

            // Decrement the count on effect removal
            _count--;
        }

        protected override void TriggerChainedEffects(GodotObject target)
        {
            base.TriggerChainedEffects(target);
            _count = 0;
        }
    }
}
