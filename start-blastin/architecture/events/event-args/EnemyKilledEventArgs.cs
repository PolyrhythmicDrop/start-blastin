using System;

namespace Events
{
    public class EnemyKilledEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public int FluxReward { get; set; }
        public int BytesReward { get; set; }
    }
}
