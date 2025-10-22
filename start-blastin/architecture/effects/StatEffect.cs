using System;
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

        public override void Apply(IStats statfulTarget)
        {
            StatManager statManager = statfulTarget.GetStatManager();
            Stat stat = statManager.GetStat(Type);
            float newValue = 0;

            if (Operation == Operation.Add)
            {
                newValue = stat.CurrentValue + Value;
            }
            else if (Operation == Operation.Multiply)
            {
                newValue = stat.CurrentValue * Value;
            }

            statManager.UpdateStat(Type, newValue);
        }

        public override void Remove(IStats statfulTarget)
        {
            StatManager statManager = statfulTarget.GetStatManager();
            Stat stat = statManager.GetStat(Type);
            float newValue = 0;

            if (Operation == Operation.Add)
            {
                newValue = stat.CurrentValue - Value;
            }
            else if (Operation == Operation.Multiply)
            {
                newValue = stat.CurrentValue / Value;
            }
        }
    }
}
