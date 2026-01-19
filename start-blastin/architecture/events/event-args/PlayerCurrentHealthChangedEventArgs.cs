using System;

namespace Events
{
    public class PlayerCurrentHealthChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public float CurrentHealth { get; }

        // The difference between this CurrentHealth value and the player's previous current health (before the change)
        public float Difference { get; }

        // Current health value's percentage of max health.
        public float Percentage { get; }

        public PlayerCurrentHealthChangedEventArgs(
            int id,
            float currentHealth,
            float diff,
            float percentage
        )
        {
            PlayerId = id;
            CurrentHealth = currentHealth;
            Difference = diff;
            Percentage = percentage;
        }
    }
}
