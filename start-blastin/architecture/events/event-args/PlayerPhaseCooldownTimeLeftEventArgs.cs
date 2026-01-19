using System;

namespace Events
{
    public class PlayerPhaseCooldownTimeLeftEventArgs : EventArgs
    {
        public int PlayerId { get; set; }

        public double TimeLeft { get; set; }
        public double TotalTime { get; set; }

        public PlayerPhaseCooldownTimeLeftEventArgs(int id, double timeLeft, double totalTime)
        {
            PlayerId = id;
            TimeLeft = timeLeft;
            TotalTime = totalTime;
        }

        public PlayerPhaseCooldownTimeLeftEventArgs()
        {
            PlayerId = 0;
            TimeLeft = 0;
            TotalTime = 0;
        }
    }
}
