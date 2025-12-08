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

        protected override void ApplyEffectToTarget(GodotObject target)
        {
            // Increment the count since we're applying the effect.
            _count++;
            base.ApplyEffectToTarget(target);
        }

        protected override void RemoveEffectFromTarget(GodotObject target)
        {
            base.RemoveEffectFromTarget(target);

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
