using System;
using System.Text.RegularExpressions;
using Entities;
using Godot;
using Interfaces;
using Stats;
using Utility;

namespace Effects
{
    [GlobalClass]
    public partial class StatEffect : Effect
    {
        [Export]
        public StatType Type { get; set; }

        [Export(PropertyHint.Range, "-100,100,0.1,or_greater,or_less")]
        public float Value { get; set; }

        [Export(PropertyHint.Enum)]
        public Operation Operation { get; set; }

        public override void ApplyEffect()
        {
            if (_target == null)
            {
                return;
            }

            // Get the current effect state for this target or create a new one
            EffectState state = GetOrCreateEffectState(_target);

            // Don't apply the effect if we're either active (if not stacking) or at max stacks (if stacking)
            if (!_stacking && state.Active || (_stacking && state.CurrentStacks >= _maxStacks))
            {
                return;
            }

            if (_target is IStats statful)
            {
                // Calculate the new value for the stat
                StatManager statMan = statful.GetStatManager();
                float currentVal = statMan.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, true);

                // Set the stat on the target
                statful.SetStat(Type, newVal);

                // Adjust the EffectState
                state.Active = true;
                if (_stacking)
                {
                    state.CurrentStacks++;
                }

                // Start the timer for the target if necessary
                if (_timed)
                {
                    StartTimer(state);
                }
            }
        }

        public override void RemoveEffect()
        {
            RemoveEffectFromTarget(_target);
        }

        protected override void RemoveEffectFromTarget(GodotObject target)
        {
            // Return immediately if there's no target or no currently active effect on the target.
            if (target == null || !_targetStates.ContainsKey(target))
            {
                return;
            }

            // Get the current effect state of the target
            EffectState state = _targetStates[target];

            // Don't remove the effect if it's not active (if not stacking) or if there are no current stacks.
            if (!_stacking && !state.Active || _stacking && state.CurrentStacks == 0)
            {
                return;
            }

            // Remove the effect from the target.
            if (_target is IStats statful)
            {
                StatManager statMan = statful.GetStatManager();
                float currentVal = statMan.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, false);
                statful.SetStat(Type, newVal);

                if (_stacking)
                {
                    state.CurrentStacks = Math.Max(0, state.CurrentStacks - 1);
                }

                if (_stacking && state.CurrentStacks <= 0 || !_stacking)
                {
                    state.Active = false;
                }
            }
        }

        /// <summary>
        /// Remove all effect stacks from the current target.
        /// </summary>
        public override void RemoveAllEffectStacks()
        {
            // Return immediately if there's no target or if the target does not have any currently active effects.
            if (_target == null || !_targetStates.ContainsKey(_target))
            {
                return;
            }

            // Get the current state
            EffectState state = _targetStates[_target];

            // If this effect doesn't stack, remove the singular effect and return
            if (!_stacking)
            {
                RemoveEffectFromTarget(_target);
                return;
            }

            // Remove all the effect stacks
            for (int i = state.CurrentStacks; i > 0; i--)
            {
                RemoveEffectFromTarget(_target);
            }
        }

        private float CalcNewStatValue(float currentValue, bool positive)
        {
            switch (Operation)
            {
                case Operation.Add:
                    if (positive)
                    {
                        return currentValue + Value;
                    }
                    else
                    {
                        return currentValue + (Value * -1);
                    }
                case Operation.Multiply:
                    if (positive)
                    {
                        return currentValue * Value;
                    }
                    else
                    {
                        return Math.Max(currentValue, 0.1f) / Value;
                    }
                default:
                    return currentValue;
            }
        }

        public string GetEffectText()
        {
            string typeName = UtilityMethods.SplitCamelCase(Type.ToString());
            string valueColor = Value > 0 ? "#25bcc6" : "#ff5470";
            string triggerString = $"On {UtilityMethods.SplitCamelCase(Trigger.ToString())}: ";

            string operation;
            if (Operation == Operation.Add)
            {
                operation = Value > 0 ? "+" : "-";
            }
            else
            {
                operation = Value > 0 ? "x" : "/";
            }

            string displayValue = Math.Abs(Value).ToString();
            return $"{triggerString}{typeName} => [color={valueColor}]{operation}{displayValue}[/color]";
        }
    }
}
