using System;
using Godot;
using Items;

namespace UI.HUD
{
    [GlobalClass]
    public partial class PluginSlot : PanelContainer
    {
        protected TextureRect _textureRect;
        protected Plugin _plugin;

        public Plugin Plugin => _plugin;

        public override void _Ready()
        {
            _textureRect = GetNode<TextureRect>("%IconRect");
        }

        public virtual void SetPlugin(Plugin plugin)
        {
            _plugin = plugin;
            _textureRect.Texture = _plugin.Icon;
        }
    }
}
