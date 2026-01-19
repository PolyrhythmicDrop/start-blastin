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
    public static class ItemDisplayFactory
    {
        public static PackedScene _itemDisplayScene = GD.Load<PackedScene>("uid://b38bjsfdg17o2");

        /// <summary>
        /// Creates an ItemSlot of a particular type based on the passed Item.
        /// </summary>
        /// <param name="item">The item to create a slot for.</param>
        /// <returns>An ItemSlot instance of a derived type.</returns>
        /// <remarks>
        /// This method does not actually add the <paramref name="item"/> to the slot!
        /// Do that after the ItemSlot has been created.
        /// </remarks>
        public static ItemDisplay CreateDisplayForItem(Item item)
        {
            ItemDisplay display = _itemDisplayScene.Instantiate<ItemDisplay>();
            display.SetItem(item);
            return display;
        }

        public static ItemDisplay CreateEmptyItemDisplay()
        {
            return _itemDisplayScene.Instantiate<ItemDisplay>();
        }
    }
}
