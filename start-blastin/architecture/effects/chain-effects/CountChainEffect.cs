using System;
using Godot;

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

        public CountChainEffect()
            : base()
        {
            ChainCondition = CountConditionMet;
        }

        public override void ApplyEffect(object source, EventArgs args)
        {
            _count++;
            base.ApplyEffect(source, args);
        }

        private bool CountConditionMet()
        {
            return _count >= Threshold;
        }
    }
}
