using System;

namespace Events
{
    public class PlayerCurrencyChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public int TotalBytes { get; }
        public int TotalFlux { get; }
        public int BytesChange { get; }
        public int FluxChange { get; }

        public PlayerCurrencyChangedEventArgs(
            int id,
            int totalBytes,
            int totalFlux,
            int bytesChange,
            int fluxChange
        )
        {
            PlayerId = id;
            TotalBytes = totalBytes;
            TotalFlux = totalFlux;
            BytesChange = bytesChange;
            FluxChange = fluxChange;
        }
    }
}
