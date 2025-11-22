using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autoloads;
using Effects;
using Entities;
using Events;
using FileIO;
using Godot;
using Items;
using Services;
using Stats;
using Utility;

namespace UI.Shop
{
    [GlobalClass]
    public partial class ShopUI : PanelContainer
    {
        private int _playerId;
        private PlayerService _service;
        private List<ShopItemContainer> _itemContainers;
        private List<Item> _itemPool = new();

        // ~~ Description section ~~
        private DescriptionPanel _descPanel;
        private RichTextLabel _descriptionLabel => _descPanel.DescriptionLabel;

        // ~~ Wave button deck ~~
        private Button _nextWaveButton;
        private Button _rerollButton;
        private Button _healButton;

        // ~~~

        private Dictionary<ShopItemContainer, Action> _containerFocusHandlers = new();

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
            _healButton = GetNode<Button>("%Heal");

            _descPanel = GetNode<DescriptionPanel>("%DescriptionPanelContainer");

            _itemContainers = new()
            {
                GetNode<ShopItemContainer>("%ShopItemContainer1"),
                GetNode<ShopItemContainer>("%ShopItemContainer2"),
                GetNode<ShopItemContainer>("%ShopItemContainer3"),
            };

            ReadyShopItemContainers();

            ConnectSignals();
            PopulateShopSlots();
            // Grab the focus to the first shop item.
            _itemContainers[0].CallDeferred(MethodName.GrabFocus);
        }

        private async void ReadyShopItemContainers()
        {
            foreach (ShopItemContainer container in _itemContainers)
            {
                container.RequestReady();
                await ToSignal(container, Node.SignalName.Ready);
            }
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

            _rerollButton.FocusEntered += RerollFocusEntered;
            _nextWaveButton.FocusEntered += NextWaveFocusEntered;
            _healButton.FocusEntered += HealFocusEntered;

            foreach (ShopItemContainer container in _itemContainers)
            {
                // container.FocusEntered += () => DisplayItemDescription(container);
                ShopItemContainer captured = container;
                Action handler = () => _descPanel.DisplayItemDescription(captured.Item);
                _containerFocusHandlers[captured] = handler;
                captured.FocusEntered += handler;

                // Connect to the shop item selected signal
                container.ItemContainerSelected += OnShopItemSelected;
            }
        }

        private void DisconnectSignals()
        {
            _rerollButton.Pressed -= RerollShop;
            _nextWaveButton.Pressed -= EventBus.Instance.RaiseStartWaveButtonPressed;

            _rerollButton.FocusEntered -= RerollFocusEntered;
            _nextWaveButton.FocusEntered -= NextWaveFocusEntered;
            _healButton.FocusEntered -= HealFocusEntered;

            foreach (var kvp in _containerFocusHandlers)
            {
                kvp.Key.FocusEntered -= kvp.Value;
            }
            _containerFocusHandlers.Clear();

            foreach (ShopItemContainer container in _itemContainers)
            {
                container.ItemContainerSelected -= OnShopItemSelected;
            }
        }

        private void RerollFocusEntered() => DisplayTickerFocusMessage(_rerollButton);

        private void NextWaveFocusEntered() => DisplayTickerFocusMessage(_nextWaveButton);

        private void HealFocusEntered() => DisplayTickerFocusMessage(_healButton);

        private void OnShopItemSelected(object source, ItemSelectedEventArgs args)
        {
            Player player = _service.GetPlayer(_playerId);
            if (player.CanBuyItem(args.Item))
            {
                EventBus.Instance.RaiseItemBought(args.Item);
                if (source is ShopItemContainer container)
                {
                    container.ItemBought();
                }
            }
            else
            {
                DebugLogger.LogMessage(
                    $"Player cannot buy item {args.Item.Name}! Returning...",
                    true,
                    true
                );
            }
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

                // Set the price label for the item to be the right color.
                Player player = _service.GetPlayer(_playerId);
                player.CanAffordItem(item, out bool flux, out bool bytes);
                container.SetBuyable(flux, bytes);
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
            // Return immediately if the item can't appear in the shop.
            if (!item.AppearsInShop)
            {
                return false;
            }

            // Does the item already exist in another item container in the shop?
            bool itemInContainer =
                _itemContainers.Find(container => container.Item == item) != null;

            // Does the player already have the item?
            bool playerHasItem = PlayerHasItem(item);

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
            ClearItemDescription();
            PopulateShopSlots();
        }

        private void DisplayItemDescription(ShopItemContainer itemContainer)
        {
            ClearItemDescription();
            Item item = itemContainer.Item;

            if (item != null)
            {
                string descString = item.Description + "\n";
                foreach (StatEffect statEffect in item.GetEffectList())
                {
                    descString += statEffect.GetEffectText() + "\n";
                }

                descString.TrimEnd('\n');
                _descriptionLabel.Text = descString;
            }
        }

        private void ClearItemDescription()
        {
            // if (_statsVBox.GetChildCount() > 0)
            // {
            //     var children = _statsVBox.GetChildren();
            //     foreach (var child in children)
            //     {
            //         _statsVBox.RemoveChild(child);
            //     }
            // }
        }

        /// <summary>
        /// Displays a message in the description bay based on the focused item.
        /// </summary>
        /// <param name="focusedControl"></param>
        private void DisplayTickerFocusMessage(Control focusedControl)
        {
            ClearItemDescription();
            if (focusedControl == _rerollButton)
            {
                _descPanel.DisplayString("Refresh the cache to see new items.");
            }
            else if (focusedControl == _healButton)
            {
                _descPanel.DisplayString("Spend flux to repair your frail human form.");
            }
            else if (focusedControl == _nextWaveButton)
            {
                _descPanel.DisplayString(
                    "Move on to the next wave and pray to whatever primitive superstition keeps you going."
                );
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
