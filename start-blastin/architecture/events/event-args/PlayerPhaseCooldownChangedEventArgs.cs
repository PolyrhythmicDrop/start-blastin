using System;

namespace Events
{
    public class PlayerPhaseCooldownChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public float NewCooldownTime { get; }
        public float OriginalCooldownTime { get; }

        public PlayerPhaseCooldownChangedEventArgs(int id, float newCooldown, float origCooldown)
        {
            PlayerId = id;
            NewCooldownTime = newCooldown;
            OriginalCooldownTime = origCooldown;
        }
    }
}
