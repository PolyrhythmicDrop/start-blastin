using System;

namespace Events
{
    public class PlayerTakeDamageEventArgs : EventArgs
    {
        public int PlayerId { get; }

        public float Damage { get; }

        public PlayerTakeDamageEventArgs(int playerId, float damage)
        {
            PlayerId = playerId;
            Damage = damage;
        }
    }
}
