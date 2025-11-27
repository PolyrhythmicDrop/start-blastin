using System;
using Items;

namespace Events
{
    public class ItemSelectedEventArgs : EventArgs
    {
        public Item Item { get; }

        public ItemSelectedEventArgs(Item item)
        {
            Item = item;
        }
    }
}
