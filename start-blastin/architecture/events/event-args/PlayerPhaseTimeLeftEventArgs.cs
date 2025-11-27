using System;

namespace Events
{
    public class PlayerPhaseTimeLeftEventArgs : EventArgs
    {
        public int PlayerId { get; set; }
        public double TimeLeft { get; set; }

        public PlayerPhaseTimeLeftEventArgs(int id, double timeLeft)
        {
            PlayerId = id;
            TimeLeft = timeLeft;
        }

        public PlayerPhaseTimeLeftEventArgs()
        {
            PlayerId = 0;
            TimeLeft = 0;
        }
    }
}
