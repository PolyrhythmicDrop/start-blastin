using System;
using System.Diagnostics;
using Godot;
using Items;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class PluginSlot : ItemSlot
    {
        protected Plugin _plugin => _item as Plugin;

        public Plugin Plugin => _plugin;

        public override void _Ready()
        {
            _icon = GetNode<TextureRect>("%IconRect");
        }

        public virtual void SetPlugin(Plugin plugin)
        {
            SetItem(plugin);
        }

        public override void SetItem(Item item)
        {
            if (item is not Plugin plugin)
            {
                DebugLogger.LogMessage(
                    $"Cannot set {Name}'s item to {item} because the item is not a plugin! {item} is {item.GetType()}",
                    true,
                    true
                );
            }
            else
            {
                base.SetItem(plugin);
            }
        }
    }
}
