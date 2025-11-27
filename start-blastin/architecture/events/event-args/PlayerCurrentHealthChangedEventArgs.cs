using System;

namespace Events
{
    public class PlayerCurrentHealthChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public float CurrentHealth { get; }

        public PlayerCurrentHealthChangedEventArgs(int id, float currentHealth)
        {
            PlayerId = id;
            CurrentHealth = currentHealth;
        }
    }
}
