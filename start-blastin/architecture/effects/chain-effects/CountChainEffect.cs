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
        private int _count = 0;

        [Export(PropertyHint.Range, "1,50,1,greater_than")]
        public int Threshold { get; set; }

        protected override bool IsChainConditionMet()
        {
            return _count >= Threshold;
        }

        public override void ApplyEffect()
        {
            base.ApplyEffect();
            _count++;
            CheckChainCondition();
        }

        protected override void TriggerChainEffects()
        {
            base.TriggerChainEffects();
            _count = 0;
        }
    }
}
