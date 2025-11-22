using System;
using Items;

namespace Events
{
    public class PlayerItemRemovedEventArgs : EventArgs
    {
        public int PlayerId { get; }
        public Item Item { get; }

        public PlayerItemRemovedEventArgs(int id, Item item)
        {
            PlayerId = id;
            Item = item;
        }
    }
}
