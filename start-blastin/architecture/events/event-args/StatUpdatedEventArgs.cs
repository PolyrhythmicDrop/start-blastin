using System;
using Stats;

namespace Events
{
    public class StatUpdatedEventArgs : EventArgs
    {
        public StatType StatType { get; }
        public Stat Stat { get; }

        public StatUpdatedEventArgs(StatType type, Stat stat)
        {
            StatType = type;
            Stat = stat;
        }
    }
}
