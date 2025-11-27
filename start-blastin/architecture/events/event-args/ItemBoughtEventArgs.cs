using System;
using Items;

namespace Events
{
    public class ItemBoughtEventArgs : EventArgs
    {
        public Item Item { get; }

        public ItemBoughtEventArgs(Item item)
        {
            Item = item;
        }
    }
}
