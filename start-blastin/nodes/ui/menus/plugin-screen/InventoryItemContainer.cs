using Godot;
using Items;
using Utility;

namespace UI.Loadout
{
    [GlobalClass]
    public partial class InventoryItemContainer : ItemContainer
    {
        private VBoxContainer _vBox;
        private PricePanelContainer _pricePanel;

        private new StyleBoxFlat _focusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>("uid://dx4qv4oioa55");

        private StyleBoxFlat _currentStyleBox;

        private Color _focusedBorderColor;
        private Color _defocusedBorderColor;

        private Tween _tween;

        public bool Empty = true;

        public override void _Ready()
        {
            base._Ready();

            _vBox = GetNode<VBoxContainer>("%VBox");
            InitializeNamePanel();

            _pricePanel = GetNode<PricePanelContainer>("%PricePanelContainer");
            InitializePricePanel();

            // Don't need to connect signals below, since we call base.Ready() above, which calls the derived class's ConnectSignals(), which calls base.ConnectSignals()...definitely not confusing
            // ConnectSignals();
        }

        public override void ConnectSignals()
        {
            ConnectSlotItemChanged(true);
            base.ConnectSignals();
        }

        public override void DisconnectSignals()
        {
            ConnectSlotItemChanged(false);
            base.DisconnectSignals();
        }

        /// <summary>
        /// Connect or disconnect to the <see cref="ItemDisplay.SlotItemChanged"/> event for the current <see cref="ItemContainer._itemDisplay"/> variable.
        /// </summary>
        /// <param name="connect">True to connect the signal, false to disconnect it.</param>
        public void ConnectSlotItemChanged(bool connect)
        {
            if (connect)
            {
                _itemDisplay.SlotItemChanged += SetPanelInfo;
            }
            else
            {
                _itemDisplay.SlotItemChanged -= SetPanelInfo;
            }
        }

        private void InitializeNamePanel()
        {
            DebugLogger.LogMessage($"Initializing the name panel for {Name}", true);
            // Set the stylebox and label settings
            _itemNamePanel.SetStyle();
            // Set the name panel as invisible until it gains focus.
            Color modColor = new(_itemNamePanel.Modulate);
            modColor.A = 0;
            _itemNamePanel.Modulate = modColor;
            // Set the name panel's pivot offset to center
            _itemNamePanel.PivotOffset = _itemNamePanel.Size / 2;
            // Set the initial scane to 2
            _itemNamePanel.Scale = new Vector2(2, 2);
            DebugLogger.LogMessage($"{Name} name panel initialization complete!", true);
        }

        private void InitializePricePanel()
        {
            // Set the mode of the price panel
            _pricePanel.SetMode(PricePanelContainer.Mode.Inventory);
            // Set the name panel as invisible until it gains focus.
            Color modColor = new(_pricePanel.Modulate);
            modColor.A = 0;
            _pricePanel.Modulate = modColor;
            // Set the name panel's pivot offset to center
            _pricePanel.PivotOffset = _pricePanel.Size / 2;
            // Set the initial scane to 2
            _pricePanel.Scale = new Vector2(2, 2);
        }

        public void SetItemDisplay(ItemDisplay display)
        {
            DebugLogger.LogMessage($"Setting item display for {Name}", true);

            // Get the index of the current display scene
            int index = _itemDisplay.GetIndex();
            // Remove the current display scene
            _vBox.RemoveChild(_itemDisplay);
            // Add and move the new display scene to the correct index.
            _vBox.AddChild(display);
            _vBox.MoveChild(display, index);
            // Set the variable to the new display scene.
            _itemDisplay = display;
            // Reconnect to the SlotItemChanged event because we changed the variable to a new object, which severed the original connection.
            ConnectSlotItemChanged(true);

            _currentStyleBox = _focusedStyleBox.Duplicate(true) as StyleBoxFlat;
            _itemDisplay.AddThemeStyleboxOverride("panel", _currentStyleBox);
            _focusedBorderColor = _currentStyleBox.BorderColor;
            _defocusedBorderColor = new Color(_focusedBorderColor);
            _defocusedBorderColor.A = 0;
            _currentStyleBox.BorderColor = _defocusedBorderColor;

            SetPanelInfo();
        }

        public override void SetItem(Item item)
        {
            // if (_itemDisplay != null)
            // {
            //     _item = _itemDisplay.Item;
            // }
        }

        private void SetPanelInfo()
        {
            if (!_itemDisplay.Empty)
            {
                _itemNamePanel.Label.Text = Item.Name;

                _pricePanel.SetLabelText(
                    Item.ScrapValue.ToString(),
                    PricePanelContainer.PriceLabel.Bytes
                );
                _pricePanel.TogglePanelVisibility(true, PricePanelContainer.PriceLabel.Bytes);
                _pricePanel.TogglePanelVisibility(false, PricePanelContainer.PriceLabel.Flux);
            }
            else
            {
                // If the item display item isn't null (but also Empty is true, as per previous step), then it's probably a "blank" item, like an empty plugin slot.
                // In this case just show the name of the item.
                if (Item != null)
                {
                    // _itemNamePanel.Label.Text = _itemDisplay.Item.Name;
                    _itemNamePanel.Label.Text = Item.Name;
                }
                _pricePanel.TogglePanelVisibility(false);
                // SetItem(null);
            }
        }

        protected override void OnFocusEnter()
        {
            _currentStyleBox.BorderColor = _focusedBorderColor;
            if (_itemNamePanel != null)
            {
                if (_tween != null)
                {
                    _tween.Kill();
                }
                _tween = CreateTween();
                _tween.SetParallel(true);
                _tween.TweenProperty(_itemNamePanel, "modulate:a", 1.0, 0.3);
                _tween.TweenProperty(_itemNamePanel, "scale", Vector2.One, 0.3);
                _tween.TweenProperty(_pricePanel, "modulate:a", 1.0, 0.3);
            }
        }

        protected override void OnFocusExit()
        {
            _currentStyleBox.BorderColor = _defocusedBorderColor;
            if (_itemNamePanel != null)
            {
                if (_tween != null)
                {
                    _tween.Kill();
                }
                _tween = CreateTween();
                _tween.SetParallel(true);
                _tween.TweenProperty(_itemNamePanel, "modulate:a", 0, 0.1);
                _tween.TweenProperty(_itemNamePanel, "scale", new Vector2(2, 2), 0.1);
                _tween.TweenProperty(_pricePanel, "modulate:a", 0, 0.1);
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
        }
    }
}
