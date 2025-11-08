using System;
using Items;

namespace Events
{
    public class ShopItemSelectedEventArgs : EventArgs
    {
        public Item Item { get; }

        public ShopItemSelectedEventArgs(Item item)
        {
            Item = item;
        }
    }
}
