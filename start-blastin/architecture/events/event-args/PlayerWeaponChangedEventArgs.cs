using System;
using Items;

namespace Events
{
    public class PlayerWeaponChangedEventArgs : EventArgs
    {
        public int PlayerId { get; }

        public readonly WeaponPlugin WeaponPlugin;

        public PlayerWeaponChangedEventArgs(int playerId, WeaponPlugin plugin)
        {
            PlayerId = playerId;
            WeaponPlugin = plugin;
        }
    }
}
