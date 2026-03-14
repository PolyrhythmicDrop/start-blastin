using System;

namespace Events
{
    public class PlayerDiedEventArgs : EventArgs
    {
        public int PlayerId { get; private set; }

        public PlayerDiedEventArgs(int id)
        {
            PlayerId = id;
        }
    }
}
