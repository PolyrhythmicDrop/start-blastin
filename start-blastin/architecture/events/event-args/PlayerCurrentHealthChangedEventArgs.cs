using System;

namespace Events
{
    public class PlayerCurrentHealthChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public float CurrentHealth { get; }

        public float Difference { get; }

        public PlayerCurrentHealthChangedEventArgs(int id, float currentHealth, float diff)
        {
            PlayerId = id;
            CurrentHealth = currentHealth;
            Difference = diff;
        }
    }
}
