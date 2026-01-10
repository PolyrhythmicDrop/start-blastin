using System;
using Stats;

namespace Events
{
    public class StatUpdatedEventArgs : EventArgs
    {
        public StatType StatType { get; }
        public Stat Stat { get; }

        public float OriginalValue { get; }

        public StatUpdatedEventArgs(StatType type, Stat stat, float originalValue)
        {
            StatType = type;
            Stat = stat;
            OriginalValue = originalValue;
        }
    }
}
