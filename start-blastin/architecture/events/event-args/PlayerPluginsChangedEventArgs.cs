using System;
using System.Collections.Generic;
using Items;

namespace Events
{
    public class PlayerPluginsChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }

        public readonly List<Plugin> Plugins;

        public PlayerPluginsChangedEventArgs(int id, List<Plugin> plugins)
        {
            PlayerId = id;
            Plugins = plugins;
        }
    }
}
