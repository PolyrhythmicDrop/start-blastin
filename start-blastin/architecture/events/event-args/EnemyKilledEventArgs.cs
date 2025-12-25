using System;
using Godot;

namespace Events
{
    public class EnemyKilledEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public int FluxReward { get; }
        public int BytesReward { get; }
        public Vector2 KillPosition { get; }

        public EnemyKilledEventArgs(int id, int flux, int bytes, Vector2 killPosition)
        {
            PlayerId = id;
            FluxReward = flux;
            BytesReward = bytes;
            KillPosition = killPosition;
        }
    }
}
