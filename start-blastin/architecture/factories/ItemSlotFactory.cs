using System;
using Godot;
using Items;
using UI.HUD;
using Utility;

namespace Factories
{
    /// <summary>
    /// Factory for creating item slots for inventory, loadout, and other uses.
    /// Can automatically create the correct type of slot for a passed item.
    /// </summary>
    public static class ItemSlotFactory
    {
        public static PackedScene _pluginSlotScene => GD.Load<PackedScene>("uid://c122xo53cyce1");
        public static PackedScene _weaponSlotScene => GD.Load<PackedScene>("uid://ccw803f7gjifl");

        /// <summary>
        /// Creates an ItemSlot of a particular type based on the passed Item.
        /// </summary>
        /// <param name="item">The item to create a slot for.</param>
        /// <returns>An ItemSlot instance of a derived type.</returns>
        /// <remarks>
        /// This method does not actually add the <paramref name="item"/> to the slot!
        /// Do that after the ItemSlot has been created.
        /// </remarks>
        public static ItemSlot CreateSlotForItem(Item item)
        {
            return item switch
            {
                WeaponPlugin => CreateWeaponSlot(),
                Plugin => CreatePluginSlot(),
                _ => _pluginSlotScene.Instantiate<PluginSlot>(),
            };
        }

        public static WeaponSlot CreateWeaponSlot() => CreateItemSlot<WeaponSlot>();

        public static PluginSlot CreatePluginSlot() => CreateItemSlot<PluginSlot>();

        public static T CreateItemSlot<T>()
            where T : ItemSlot
        {
            if (typeof(T) == typeof(PluginSlot))
            {
                return _pluginSlotScene.Instantiate<PluginSlot>() as T;
            }
            else if (typeof(T) == typeof(WeaponSlot))
            {
                return _weaponSlotScene.Instantiate<WeaponSlot>() as T;
            }
            else
            {
                DebugLogger.LogMessage(
                    $"Could not create item slot because {typeof(T)} is not an accepted type!",
                    true,
                    true
                );
                return null;
            }
        }
    }
}
