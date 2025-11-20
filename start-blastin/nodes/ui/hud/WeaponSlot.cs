using System;
using Godot;
using Items;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class WeaponSlot : PluginSlot
    {
        public new WeaponPlugin Plugin => _plugin as WeaponPlugin;

        public WeaponPlugin Weapon => Plugin;

        public void SetWeapon(WeaponPlugin weapon)
        {
            SetItem(weapon);
        }

        public override void SetItem(Item item)
        {
            DebugLogger.LogMessage($"Setting item in WeaponSlot...{item}", true);
            try
            {
                if (_plugin == null || item != _plugin)
                {
                    if (item is not WeaponPlugin)
                    {
                        throw new ArgumentException(
                            $"WeaponSlot can only accept WeaponPlugin types! Recieved a {item?.GetType().Name} instead."
                        );
                    }
                    else
                    {
                        base.SetItem(item);
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"Projectile type already equipped! Attempted to equip {item?.GetType().Name}"
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
