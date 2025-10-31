using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Autoloads;
using Entities;
using FileIO;
using Godot;
using Items;
using Services;
using Utility;

namespace UI.Shop
{
    [GlobalClass]
    public partial class ShopUI : Control
    {
        private int _playerId;
        private PlayerService _service;
        private List<ShopItemContainer> _itemContainers;
        private List<Item> _itemPool = new();
        private Button _nextWaveButton;
        private Button _rerollButton;
        private Button _healButton;

        public void LoadItemPool() =>
            PoolLoader.LoadResourcePool(_itemPool, "res://resources/items/", true);

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Calling _Ready...", true);

            if (_itemPool.Count <= 0)
            {
                LoadItemPool();
            }

            _nextWaveButton = GetNode<Button>("%NextWaveButton");
            _rerollButton = GetNode<Button>("%RerollButton");
            _healButton = GetNode<Button>("%Heal50");

            _itemContainers = new()
            {
                GetNode<ShopItemContainer>("%ShopItemContainer1"),
                GetNode<ShopItemContainer>("%ShopItemContainer2"),
                GetNode<ShopItemContainer>("%ShopItemContainer3"),
            };

            ConnectSignals();
            PopulateShopSlots();
            // Grab the focus to the first shop item.
            _itemContainers[0].CallDeferred(MethodName.GrabFocus);
        }

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            _service = ServiceManager.Instance.GetService<PlayerService>();
        }

        private void ConnectSignals()
        {
            _rerollButton.Pressed += RerollShop;
            _nextWaveButton.Pressed += EventBus.Instance.RaiseStartWaveButtonPressed;
        }

        private void DisconnectSignals()
        {
            _rerollButton.Pressed -= RerollShop;
            _nextWaveButton.Pressed -= EventBus.Instance.RaiseStartWaveButtonPressed;
        }

        /// <summary>
        /// Populates all shop slots with items for the player to buy.
        /// </summary>
        private void PopulateShopSlots()
        {
            foreach (ShopItemContainer container in _itemContainers)
            {
                GD.Print("Retrieving item from pool...");
                Item item = GetItemFromPool();

                while (!CanShowItem(item))
                {
                    item = GetItemFromPool();
                }

                container.SetItem(item);
            }
        }

        /// <summary>
        /// Retrieves a weighted item from the item pool.
        /// </summary>
        /// <returns>An Item from the pool.</returns>
        private Item GetItemFromPool()
        {
            if (_itemPool == null || _itemPool.Count == 0)
            {
                return null;
            }

            // Get total weight of all items
            int totalWeight = 0;
            foreach (Item item in _itemPool)
            {
                totalWeight += (int)item.Rarity;
            }

            int randomValue = GD.RandRange(0, totalWeight - 1);

            int currentWeight = 0;

            foreach (Item item in _itemPool)
            {
                currentWeight += (int)item.Rarity;
                if (randomValue < currentWeight)
                {
                    DebugLogger.LogMessage(
                        $"{item.GetType().Name} {item.Name} retrieved from pool!",
                        true
                    );
                    return item;
                }
            }

            // Return null if we couldn't find a matching item in the _itemPool
            DebugLogger.LogMessage($"Could not load an item from the item pool!", true, true);
            return null;
        }

        /// <summary>
        /// Runs checks to determine whether an item can appear in the shop.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the item can be displayed in the shop. False if not.</returns>
        private bool CanShowItem(Item item)
        {
            // Does the item already exist in another item container in the shop?
            bool itemInContainer =
                _itemContainers.Find(container => container.Item == item) != null;

            bool playerHasItem = PlayerHasItem(item);

            // Return true if both of the above are false
            return !itemInContainer && !playerHasItem;
        }

        /// <summary>
        /// Checks if the player has the passed Item in their inventory.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the player has the item. False if the player does not.</returns>
        /// <remarks>
        /// Currently, this method only checks for Plugins, since the player can have multiples of the same modifier.
        /// Expand this to check for consumables or other exclusive items later.
        /// </remarks>
        private bool PlayerHasItem(Item item)
        {
            Player player = _service.GetPlayer(_playerId);
            if (item is Plugin plugin)
            {
                return player.HasPlugin(plugin);
            }
            else
            {
                return false;
            }
        }

        private void ClearItemContainers()
        {
            foreach (ShopItemContainer container in _itemContainers)
            {
                container.ClearItem();
            }
        }

        private void RerollShop()
        {
            GD.Print($"Rerolling shop...");
            ClearItemContainers();
            PopulateShopSlots();
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
