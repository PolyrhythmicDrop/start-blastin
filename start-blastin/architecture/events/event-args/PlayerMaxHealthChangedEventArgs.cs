using System;

namespace Events
{
    public class PlayerMaxHealthChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public float MaxHealth { get; }

        public PlayerMaxHealthChangedEventArgs(int id, float maxHealth)
        {
            PlayerId = id;
            MaxHealth = maxHealth;
        }
    }
}
