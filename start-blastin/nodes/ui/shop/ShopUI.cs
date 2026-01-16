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

        private RichTextLabel _healLabel;
        private string _healIconKey = "healIcon";
        private Texture2D _healTexture = ResourceLoader.Load<Texture2D>(
            "res://assets/icons/currency/flux-icon.png"
        );

        private int _healPrice = 0;

        // ~~~

        private Dictionary<ShopItemContainer, Action> _containerFocusHandlers = new();

        /// <summary>
        /// The object that was last in focus. Used for switching back focus after the plugin screen.
        /// </summary>
        private Node _lastFocused;

        public bool Active { get; set; }

        public void LoadItemPool()
        {
            PoolLoader.LoadResourcePool(_itemPool, "res://resources/items/", true);
            // Cull all items that can't appear in the shop
            List<Item> unsellable = _itemPool.FindAll(item => !item.AppearsInShop);
            foreach (Item item in unsellable)
            {
                _itemPool.Remove(item);
            }
            unsellable.Clear();
        }

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Calling _Ready...", true);

            _nextWaveButton = GetNode<Button>("%NextWaveButton");
            _rerollButton = GetNode<Button>("%RerollButton");
            _healButton = GetNode<Button>("%HealButton");
            _healLabel = GetNode<RichTextLabel>("%HealLabel");

            _descPanel = GetNode<DescriptionPanel>("%DescriptionPanelContainer");

            _itemContainers = new()
            {
                GetNode<ShopItemContainer>("%ShopItemContainer1"),
                GetNode<ShopItemContainer>("%ShopItemContainer2"),
                GetNode<ShopItemContainer>("%ShopItemContainer3"),
            };

            ConnectSignals();
        }

        private void SetHealPrice()
        {
            Player player = _service.GetPlayer(_playerId);
            _healPrice = (int)MathF.Round(player.MaxHealth - player.CurrentHealth, 0) * 100;
            // DebugLogger.LogMessage($"{_healIconKey}", true);

            _healLabel.Text = "";
            _healLabel.AppendText("Heal");
            _healLabel.Newline();
            _healLabel.AddImage(_healTexture, width: 32, height: 32);
            _healLabel.AppendText(_healPrice.ToString());
        }

        public void StockShop()
        {
            if (_itemPool.Count <= 0)
            {
                LoadItemPool();
            }
            ReadyShopItemContainers();
            PopulateShopSlots();
        }

        public void ToggleActivate(bool activate)
        {
            if (activate)
            {
                Active = true;
                RefreshAllAffordability(_playerId);
                SetHealPrice();
                if (_lastFocused != null)
                {
                    _lastFocused.CallDeferred(MethodName.GrabFocus);
                }
                else
                {
                    _itemContainers[0].CallDeferred(MethodName.GrabFocus);
                }
            }
            else
            {
                Active = false;
            }
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

        #region Event Handling
        private void ConnectSignals()
        {
            _rerollButton.Pressed += RerollShop;
            _nextWaveButton.Pressed += EventBus.Instance.RaiseStartWaveButtonPressed;
            _healButton.Pressed += OnHealButtonPressed;
            EventBus.Instance.PlayerMaxHealthChanged += (source, args) =>
            {
                SetHealPrice();
            };

            _rerollButton.FocusEntered += RerollFocusEntered;
            _nextWaveButton.FocusEntered += NextWaveFocusEntered;
            _healButton.FocusEntered += HealFocusEntered;

            // Connect player variables for instant refreshing
            EventBus.Instance.PlayerCurrencyChanged += OnPlayerCurrencyChanged;
            EventBus.Instance.PlayerItemRemoved += OnPlayerItemRemoved;
            EventBus.Instance.PlayerPluginEquipped += OnPlayerPluginEquipped;

            foreach (ShopItemContainer container in _itemContainers)
            {
                // container.FocusEntered += () => DisplayItemDescription(container);
                ShopItemContainer captured = container;
                Action handler = () =>
                {
                    _descPanel.DisplayItemDescription(captured.Item);
                    _lastFocused = captured;
                };
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

            EventBus.Instance.PlayerCurrencyChanged -= OnPlayerCurrencyChanged;
            EventBus.Instance.PlayerItemRemoved -= OnPlayerItemRemoved;
            EventBus.Instance.PlayerPluginEquipped -= OnPlayerPluginEquipped;

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

        private void RerollFocusEntered()
        {
            DisplayTickerFocusMessage(_rerollButton);
            _lastFocused = _rerollButton;
        }

        private void NextWaveFocusEntered()
        {
            DisplayTickerFocusMessage(_nextWaveButton);
            _lastFocused = _nextWaveButton;
        }

        private void HealFocusEntered()
        {
            DisplayTickerFocusMessage(_healButton);
            _lastFocused = _healButton;
        }

        private void RerollShop()
        {
            // ClearItemContainers();
            PopulateShopSlots();
        }

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

        private void OnPlayerCurrencyChanged(object source, PlayerCurrencyChangedEventArgs args) =>
            RefreshAllAffordability(args.PlayerId);

        private void OnPlayerItemRemoved(object source, PlayerItemRemovedEventArgs args) =>
            RefreshAllAffordability(args.PlayerId);

        private void OnPlayerPluginEquipped(object source, PlayerPluginEquippedEventArgs args) =>
            RefreshAllAffordability(args.PlayerId);

        private void OnHealButtonPressed()
        {
            Player player = _service.GetPlayer(_playerId);
            if (player.Flux >= _healPrice)
            {
                player.Flux -= _healPrice;
                player.Heal(player.MaxHealth);
                SetHealPrice();
            }
        }

        private void RefreshAllAffordability(int argsId)
        {
            if (Active)
            {
                if (_playerId == argsId)
                {
                    foreach (ShopItemContainer container in _itemContainers)
                    {
                        if (container != null && container.Item != null)
                        {
                            SetContainerAffordability(container);
                        }
                    }
                }
            }
        }
        #endregion

        #region Shop Slots

        /// <summary>
        /// Populates all shop slots with items for the player to buy.
        /// </summary>
        private void PopulateShopSlots()
        {
            foreach (ShopItemContainer container in _itemContainers)
            {
                Item item = GetItemFromPool();

                while (!CanShowItem(item))
                {
                    item = GetItemFromPool();
                }

                container.SetItem(item);
                SetContainerAffordability(container);
            }
        }

        private void SetContainerAffordability(ShopItemContainer container)
        {
            Player player = _service.GetPlayer(_playerId);
            player.CanAffordItem(container.Item, out bool flux, out bool bytes);
            container.SetBuyable(flux, bytes, player.CanBuyItem(container.Item));
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

            // int randomValue = GD.RandRange(0, totalWeight - 1);
            int randomValue = RNG.GetRandomInt(0, totalWeight - 1);

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

        private void DisplayItemDescription(ShopItemContainer itemContainer)
        {
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

        /// <summary>
        /// Displays a message in the description bay based on the focused item.
        /// </summary>
        /// <param name="focusedControl"></param>
        private void DisplayTickerFocusMessage(Control focusedControl)
        {
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
        #endregion

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
