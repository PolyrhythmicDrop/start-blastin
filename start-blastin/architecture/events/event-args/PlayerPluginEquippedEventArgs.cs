using System;
using Items;

namespace Events
{
    public class PlayerPluginEquippedEventArgs : EventArgs
    {
        public int PlayerId { get; }

        public readonly Plugin NewPlugin;

        public PlayerPluginEquippedEventArgs(int id, Plugin newPlugin)
        {
            PlayerId = id;
            NewPlugin = newPlugin;
        }
    }
}
