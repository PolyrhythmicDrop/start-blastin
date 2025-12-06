using System;
using System.Text.RegularExpressions;
using Enemies;
using Entities;
using Godot;
using Interfaces;
using Stats;

namespace Effects
{
    [GlobalClass]
    [Tool]
    public partial class StatEffect : Effect
    {
        [Export]
        public StatType Type { get; set; }

        [Export(PropertyHint.Range, "-100,100,0.1,or_greater,or_less")]
        public float Value { get; set; }

        [Export(PropertyHint.Enum)]
        public Operation Operation { get; set; }

        public override void ApplyEffect(object source, EventArgs args)
        {
            // Don't apply the effect if we're either active (if not stacking) or at max stacks (if stacking)
            if (!_stacking && _active || (_stacking && _currentStacks >= _maxStacks))
            {
                return;
            }

            // If the _target is not already set to the Player, set the _target based on the passed args.
            if (_target is not Player || _target == null)
            {
                SetTarget(args);
            }

            if (_target is IStats statful)
            {
                StatManager statMan = statful.GetStatManager();
                float currentVal = statMan.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, true);
                statful.SetStat(Type, newVal);
                _active = true;
                if (_stacking)
                {
                    CurrentStacks++;
                }
            }
        }

        public override void RemoveEffect()
        {
            // Don't remove the effect if it's not active (if not stacking) or if there are no current stacks.
            if (!_stacking && !_active || _stacking && _currentStacks == 0)
            {
                return;
            }

            if (_target is IStats statful)
            {
                StatManager statMan = statful.GetStatManager();
                float currentVal = statMan.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, false);
                statful.SetStat(Type, newVal);

                CurrentStacks = Math.Max(0, CurrentStacks - 1);

                if (_stacking && _currentStacks == 0 || !_stacking)
                {
                    _active = false;
                }
            }
        }

        /// <summary>
        /// Remove all effect stacks.
        /// </summary>
        public override void RemoveAllEffectStacks()
        {
            if (!_stacking)
            {
                RemoveEffect();
                return;
            }
            else
            {
                for (int i = 0; i < _currentStacks; i++)
                {
                    RemoveEffect();
                }
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
            string typeName = SplitCamelCase(Type.ToString());
            string valueColor = Value > 0 ? "#25bcc6" : "#ff5470";

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
            return $"{typeName} => [color={valueColor}]{operation}{displayValue}[/color]";
        }

        public string SplitCamelCase(string str)
        {
            return Regex.Replace(
                Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }
    }
}
