using System;
using System.Collections.Generic;
using Godot;
using Utility;

namespace Effects
{
    /// <summary>
    /// Effect that triggers attached effects when trigger conditions are met.
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

        protected abstract bool IsChainConditionMet();

        public virtual void CheckChainCondition()
        {
            if (IsChainConditionMet())
            {
                TriggerChainEffects();
            }
        }

        protected virtual void TriggerChainEffects()
        {
            List<Effect> chainedEffects = _effects.FindAll(fx => fx.Trigger == Trigger.Chain);

            foreach (Effect effect in chainedEffects)
            {
                effect.ApplyEffect(_target);
            }
        }
    }
}
