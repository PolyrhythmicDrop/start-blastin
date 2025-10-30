using System;

namespace Events
{
    public class EnemyKilledEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public int FluxReward { get; }
        public int BytesReward { get; }

        public EnemyKilledEventArgs(int id, int flux, int bytes)
        {
            PlayerId = id;
            FluxReward = flux;
            BytesReward = bytes;
        }
    }
}
