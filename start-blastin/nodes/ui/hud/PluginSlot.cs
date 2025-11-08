using System;
using Godot;
using Items;

namespace UI.HUD
{
    [GlobalClass]
    public partial class PluginSlot : PanelContainer
    {
        protected Plugin _plugin;

        public Plugin Plugin => _plugin;

        public virtual void SetPlugin(Plugin plugin)
        {
            _plugin = plugin;
        }
    }
}
