using System;
using System.Collections.Generic;
using Godot;

namespace Effects
{
    /// <summary>
    /// Effect that triggers attached effects when trigger conditions are met.
    /// </summary>
    [GlobalClass]
    public abstract partial class ChainEffect : Effect
    {
        protected List<Effect> _effects;

        [Export]
        public Godot.Collections.Array<Effect> Effects
        {
            get => [.. _effects];
            set => _effects = [.. value];
        }

        /// <summary>
        /// Function that returns a bool. When true, the chain condition has been met and the attached effects are triggered.
        /// </summary>
        protected Func<bool> ChainCondition;

        protected event Action ChainConditionMet;

        public ChainEffect()
        {
            ChainConditionMet += OnChainConditionMet;
        }

        ~ChainEffect()
        {
            ChainConditionMet -= OnChainConditionMet;
        }

        protected virtual void OnChainConditionMet()
        {
            List<Effect> chainedEffects = _effects.FindAll(fx => fx.Trigger == Trigger.Chain);

            foreach (Effect effect in chainedEffects)
            {
                ApplyEffect(_target);
            }
        }
    }
}
