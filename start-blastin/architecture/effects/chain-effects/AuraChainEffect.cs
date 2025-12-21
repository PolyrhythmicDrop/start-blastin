using System;
using Godot;

namespace Effects
{
    public partial class AuraChainEffect : ChainEffect
    {
        protected class AuraChainEffectState : EffectState
        {
            internal AuraChainEffectState(AuraChainEffect parent)
                : base(parent)
            {
                _parent = parent;
            }

            internal PackedScene _auraScene = GD.Load<PackedScene>("uid://t6wcme7sc7j7");
        }

        public enum AuraEffectCondition
        {
            BodyEnter,
            BodyExit,
            BodyInside,
        }

        [Export]
        public AuraEffectCondition ConditionType { get; set; }

        [Export(PropertyHint.Range, "1,1000,10,or_greater")]
        public float AuraRadius { get; set; }

        protected override EffectState CreateEffectState()
        {
            return new AuraChainEffectState(this);
        }

        protected override bool IsChainConditionMet(GodotObject target)
        {
            throw new NotImplementedException();
        }
    }
}
