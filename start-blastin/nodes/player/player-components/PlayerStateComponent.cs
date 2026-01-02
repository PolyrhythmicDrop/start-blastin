using System.Linq;
using Entities;
using Godot;
using Interfaces;
using Items;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class PlayerStateComponent : Node, IPlayerComponent
    {
        private Player _player;

        public bool Phasing = false;
        public bool PhaseReady = true;
        public bool Dying = false;
        public bool DeflectActive = false;

        public void Initialize(Player player)
        {
            _player = player;
        }

        /// <summary>
        /// Checks if the player is able to phase.
        /// </summary>
        /// <returns>True if phase is not on cooldown, the player is not currently phasing, and the player is not dying or dead.</returns>
        public bool CanPhase()
        {
            return !Phasing && !Dying && PhaseReady;
        }

        /// <summary>
        /// Checks to see if the player can purchase the passed item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the player is able to buy and equip the item, false if not.</returns>
        public bool CanBuyItem(Item item)
        {
            bool canAfford = CanAffordItem(item);
            // bool noDupePlugins = _plugins.Contains(item) ? false : true;
            bool noDupePlugins =
                _player.Inventory.EquippedPlugins.FirstOrDefault(plugin =>
                    plugin.ResourceName == item.ResourceName
                ) == null
                    ? true
                    : false;
            bool noDupeWeapon = !_player.Inventory.WeaponPlugin.Equals(item);
            bool freeSlot = (_player.Inventory.EquippedPlugins.Count + 1) <= _player.PluginSlots;

            return canAfford && noDupePlugins && noDupeWeapon && freeSlot;
        }

        /// <summary>
        /// Checks if the player can afford an item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the player can afford the item, false if not.</returns>
        public bool CanAffordItem(Item item)
        {
            return item.FluxCost <= _player.Flux && item.ByteCost <= _player.Bytes;
        }

        /// <summary>
        /// Checks to see if the player can purchase the passed item based on its flux and byte cost.
        /// Contains out parameters to return specific bools for each currency type.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="flux">Output that indicates whether or not the player has enough flux.</param>
        /// <param name="bytes">Output that indicates whether or not the player has enough bytes.</param>
        /// <returns>True if the player is able to buy and equip the item, false if not.</returns>
        public bool CanAffordItem(Item item, out bool flux, out bool bytes)
        {
            flux = item.FluxCost <= _player.Flux;
            bytes = item.ByteCost <= _player.Bytes;
            return flux && bytes;
        }
    }
}
