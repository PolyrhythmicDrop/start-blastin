using System;

namespace Events
{
    public class PlayerCurrencyChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public int Bytes { get; }
        public int Flux { get; }

        public PlayerCurrencyChangedEventArgs(int id, int bytes, int flux)
        {
            PlayerId = id;
            Bytes = bytes;
            Flux = flux;
        }
    }
}
