using System;
using Items;

namespace Events
{
    public class ItemScrappedEventArgs : EventArgs
    {
        public Item Item { get; }

        public ItemScrappedEventArgs(Item item)
        {
            Item = item;
        }
    }
}
