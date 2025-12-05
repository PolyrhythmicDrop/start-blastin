using System;
using System.Text.RegularExpressions;
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

        public string GetEffectText()
        {
            // string typeColor = "#6e5181";
            string typeName = SplitCamelCase(Type.ToString());
            // string type = $"[color={typeColor}]{typeName}[/color]";
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
