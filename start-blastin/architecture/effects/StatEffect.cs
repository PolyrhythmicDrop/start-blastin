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
            if (_active)
            {
                return;
            }

            StatManager statMan;
            // If the _target is not already set to the Player, set the _target based on the passed args.
            if (_target is not Player || _target == null)
            {
                SetTarget(args);
            }

            if (_target is IStats statful)
            {
                statMan = statful.GetStatManager();
                float currentVal = statMan.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, true);
                statful.SetStat(Type, newVal);
                _active = true;
            }
        }

        public override void RemoveEffect()
        {
            if (!_active)
            {
                return;
            }

            if (_target is IStats statful)
            {
                StatManager statMan = statful.GetStatManager();
                float currentVal = statMan.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, false);
                statful.SetStat(Type, newVal);
                _active = false;
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
