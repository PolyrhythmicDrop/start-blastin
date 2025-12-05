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
            StatManager statManager;
            // If the current target is a player...
            if (_target is Player player)
            {
                statManager = player.GetStatManager();
                float currentVal = statManager.GetStat(Type).CurrentValue;
                float newVal = CalcNewStatValue(currentVal, true);
                player.SetStat(Type, newVal);
            }
            // If the target type is not self, then it's something other than a player
            else if (Target != TargetType.Self)
            {
                // Set the target based on the event type passed
                SetTarget(args);
                if (_target is EnemyNode enemy)
                {
                    statManager = enemy.GetStatManager();
                    float currentVal = statManager.GetStat(Type).CurrentValue;
                    float newVal = CalcNewStatValue(currentVal, true);
                    enemy.SetStat(Type, newVal);
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
