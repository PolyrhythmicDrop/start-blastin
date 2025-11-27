using System;

namespace Events
{
    public class WaveStartedEventArgs : EventArgs
    {
        public int Wave { get; }

        public WaveStartedEventArgs(int wave)
        {
            Wave = wave;
        }
    }
}
