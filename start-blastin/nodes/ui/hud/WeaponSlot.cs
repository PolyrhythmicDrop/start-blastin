using System;
using Godot;
using Items;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class WeaponSlot : PluginSlot
    {
        private TextureRect _textureRect;
        public new WeaponPlugin Plugin => _plugin as WeaponPlugin;

        public WeaponPlugin Weapon => Plugin;

        public override void _Ready()
        {
            _textureRect = GetNode<TextureRect>("%IconRect");
        }

        public override void SetPlugin(Plugin plugin)
        {
            try
            {
                if (plugin != _plugin)
                {
                    if (plugin is WeaponPlugin weaponPlugin)
                    {
                        _plugin = weaponPlugin;
                        _textureRect.Texture = Weapon.Icon;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"WeaponSlot can only accept WeaponPlugin types! Recieved a {plugin?.GetType().Name} instead."
                        );
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"Projectile type already equipped! Attempted to equip {plugin?.GetType().Name}"
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }
    }
}
