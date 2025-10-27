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

namespace Shop
{
    [GlobalClass]
    public partial class ShopUI : Control
    {
        private int _playerId;
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

            LoadItemPool();

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
        }

        private void ConnectSignals()
        {
            // Connect reroll button signal
            Callable rerollCallable = Callable.From(RerollShop);
            if (!_rerollButton.IsConnected(Button.SignalName.Pressed, rerollCallable))
            {
                _rerollButton.Connect(Button.SignalName.Pressed, rerollCallable);
                GD.Print($"Reroll button connected!");
            }

            // Connect next wave signal
            Callable wavePressedCallable = Callable.From(() =>
            {
                EventBus.Instance.EmitSignal(EventBus.SignalName.StartWaveButtonPressed);
            });
            if (!_nextWaveButton.IsConnected(Button.SignalName.Pressed, wavePressedCallable))
            {
                _nextWaveButton.Connect(Button.SignalName.Pressed, wavePressedCallable);
                GD.Print($"Next wave button connected!");
            }

            // Connect heal button signal
            // Callable playerHealCallable = Callable.From(() =>
            // {
            //     float healAmount = _player.MaxHealth * 0.5f;
            //     _player.Heal(healAmount);
            // });
            // if (!_healButton.IsConnected(Button.SignalName.Pressed, playerHealCallable))
            // {
            //     _healButton.Connect(Button.SignalName.Pressed, playerHealCallable);
            // }
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

            PlayerService service = ServiceManager.Instance.GetService<PlayerService>();

            // Is the item a plugin, and, if so, does the player already have it?
            bool playerHasPlugin = item is Plugin plugin
                ? service.PlayerHasPlugin(_playerId, plugin)
                : false;

            // Return true if both of the above are false
            return !itemInContainer && !playerHasPlugin;
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
    }
}
