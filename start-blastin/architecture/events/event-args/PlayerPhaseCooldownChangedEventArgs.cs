using System;

namespace Events
{
    public class PlayerPhaseCooldownChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public float CooldownTime { get; }

        public PlayerPhaseCooldownChangedEventArgs(int id, float cooldown)
        {
            PlayerId = id;
            CooldownTime = cooldown;
        }
    }
}
