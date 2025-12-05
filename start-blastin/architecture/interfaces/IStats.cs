using System;
using Events;
using Stats;

namespace Interfaces
{
    public interface IStats
    {
        StatManager GetStatManager();

        void OnStatUpdated(object source, StatUpdatedEventArgs args);

        void SetStat(StatType type, float value);
    }
}
