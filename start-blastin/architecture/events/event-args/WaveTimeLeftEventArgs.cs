using System;

namespace Events
{
    public class WaveTimeLeftEventArgs : EventArgs
    {
        public double TimeLeft { get; set; }
        public double TotalTime { get; set; }
    }
}
