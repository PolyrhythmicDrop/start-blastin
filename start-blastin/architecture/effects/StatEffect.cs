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
    [Tool]
    public partial class StatEffect : Effect
    {
        [Export]
        public StatType Type { get; set; }

        [Export(PropertyHint.Range, "-100,100,0.1,or_greater,or_less")]
        public float Value { get; set; }

        [Export(PropertyHint.Enum)]
        public Operation Operation { get; set; }

        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (target is not IStats statful)
            {
                return;
            }

            // Calculate the new value for the stat
            StatManager statMan = statful.GetStatManager();
            float currentVal = statMan.GetStat(Type).CurrentValue;
            float newVal = CalcNewStatValue(currentVal, true);

            // Set the stat on the target
            statful.SetStat(Type, newVal);
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            // // Return immediately if there's no target or no currently active effect on the target.
            if (target is not IStats statful)
            {
                return;
            }

            // Calculate and apply new stat values
            StatManager statMan = statful.GetStatManager();
            float currentVal = statMan.GetStat(Type).CurrentValue;
            float newVal = CalcNewStatValue(currentVal, false);
            statful.SetStat(Type, newVal);
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
