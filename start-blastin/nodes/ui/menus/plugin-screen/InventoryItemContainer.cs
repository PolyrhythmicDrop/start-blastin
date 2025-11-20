using Godot;
using Items;
using Utility;

namespace UI.Loadout
{
    [GlobalClass]
    public partial class InventoryItemContainer : ItemContainer
    {
        private ItemSlot _itemSlot;
        private VBoxContainer _vBox;
        private PricePanelContainer _pricePanel;
        public bool Empty = true;
        public ItemSlot Slot => _itemSlot;

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Ready called!", true);
            _vBox = GetNode<VBoxContainer>("%VBox");

            _itemNamePanel = GetNode<ItemNamePanelContainer>("%ItemNamePanel");
            InitializeNamePanel();

            _pricePanel = GetNode<PricePanelContainer>("%PricePanelContainer");
            InitializePricePanel();

            ConnectSignals();
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

        public void SetItemSlot(ItemSlot itemSlot)
        {
            DebugLogger.LogMessage($"Setting item slot for {Name}", true);
            _itemSlot = itemSlot;
            SetPanelInfo();
            _vBox.AddChild(_itemSlot);
            // Move the slot to be beneath the namePanel
            int nameIndex = _itemNamePanel.GetIndex();
            _vBox.MoveChild(_itemSlot, nameIndex + 1);
        }

        private void SetPanelInfo()
        {
            if (_itemSlot.Item != null)
            {
                _itemNamePanel.Label.Text = _itemSlot.Item.Name;
                _pricePanel.SetLabelText(
                    _itemSlot.Item.ScrapValue.ToString(),
                    PricePanelContainer.PriceLabel.Bytes
                );
                _pricePanel.TogglePanelVisibility(true, PricePanelContainer.PriceLabel.Bytes);
                _pricePanel.TogglePanelVisibility(false, PricePanelContainer.PriceLabel.Flux);
            }
            else
            {
                _itemNamePanel.Label.Text = "Empty";
                _pricePanel.TogglePanelVisibility(false);
            }
        }

        protected override void OnFocusEnter()
        {
            DebugLogger.LogMessage($"{Name} focus entered!");
            if (_itemNamePanel != null)
            {
                Tween tween = CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(_itemNamePanel, "modulate:a", 1.0, 0.3);
                tween.TweenProperty(_itemNamePanel, "scale", Vector2.One, 0.3);
                tween.TweenProperty(_itemSlot, "scale", Vector2.One, 0.3);
                tween.TweenProperty(_pricePanel, "modulate:a", 1.0, 0.3);
            }
        }

        protected override void OnFocusExit()
        {
            if (_itemNamePanel != null)
            {
                Tween tween = _itemNamePanel.CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(_itemNamePanel, "modulate:a", 0, 0.3);
                tween.TweenProperty(_itemNamePanel, "scale", new Vector2(2, 2), 0.3);
                tween.TweenProperty(_itemSlot, "scale", new Vector2(0.8f, 0.8f), 0.3);
                tween.TweenProperty(_pricePanel, "modulate:a", 0, 0.3);
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
        }
    }
}
