using System;
using System.Diagnostics;
using Autoloads;
using Events;
using Godot;
using Interfaces;
using Items;
using Utility;

namespace UI.Shop
{
    [GlobalClass]
    public partial class ShopItemContainer : PanelContainer, IListener
    {
        private Item _item;
        private TextureRect _textureRect;
        private Label _itemNameLabel;
        private StyleBoxFlat _defocusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>(
                "res://resources/themes/styleboxes/item-container-defocused-stylebox.tres"
            );
        private StyleBoxFlat _focusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>(
                "res://resources/themes/styleboxes/item-container-focused-stylebox.tres"
            );

        // ~ Nodes ~ //
        private PanelContainer _bytePanelContainer;
        private PanelContainer _fluxPanelContainer;
        private Label _byteLabel;
        private Label _fluxLabel;

        private Color _itemColor;
        public Item Item => _item;

        public event EventHandler<ShopItemSelectedEventArgs> ShopItemSelected;

        public override void _Ready()
        {
            GD.Print($"{Name} ready called!");
            _textureRect = GetNode<TextureRect>("%ItemIcon");
            _itemNameLabel = GetNode<Label>("%ItemNameLabel");
            _bytePanelContainer = GetNode<PanelContainer>("%BytePanelContainer");
            _fluxPanelContainer = GetNode<PanelContainer>("%FluxPanelContainer");
            _byteLabel = GetNode<Label>("%ByteLabel");
            _fluxLabel = GetNode<Label>("%FluxLabel");

            ConnectSignals();
        }

        public override void _EnterTree()
        {
            base._EnterTree();
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (Input.IsActionJustPressedByEvent("ui_accept", @event))
            {
                DebugLogger.LogMessage($"ui_accept Action just pressed!", true);
                ItemSelected();
                AcceptEvent();
            }
        }

        public void ConnectSignals()
        {
            FocusEntered += OnFocusEnter;
            FocusExited += OnFocusExit;
        }

        public void DisconnectSignals()
        {
            FocusEntered -= OnFocusEnter;
            FocusExited -= OnFocusExit;
        }

        public override void _Process(double delta) { }

        /// <summary>
        /// Sets the item that belongs in the container.
        /// </summary>
        /// <param name="item">The item to place in the container.</param>
        public void SetItem(Item item)
        {
            if (_item != null)
            {
                ClearItem();
            }

            _item = item;
            _itemColor = new("FLORAL_WHITE");

            switch (item.Rarity)
            {
                case Rarity.Common:
                default:
                    break;
                case Rarity.Uncommon:
                    _itemColor = new("#78d8b7");
                    break;
                case Rarity.Rare:
                    _itemColor = new("#fdfe89");
                    break;
                case Rarity.Legendary:
                    _itemColor = new("#ff5470");
                    break;
            }

            _itemNameLabel.LabelSettings.FontColor = _itemColor;
            _itemNameLabel.Text = _item.Name;

            _textureRect.Texture = _item.Icon;

            SetItemPriceLabels();
        }

        private void SetItemPriceLabels()
        {
            int flux = _item.FluxCost;
            int bytes = _item.ByteCost;

            _fluxLabel.Text = flux.ToString("N0");
            _byteLabel.Text = bytes.ToString("N0");

            if (flux <= 0)
            {
                _fluxPanelContainer.Visible = false;
            }
            else
            {
                _fluxPanelContainer.Visible = true;
            }

            if (bytes <= 0)
            {
                _bytePanelContainer.Visible = false;
            }
            else
            {
                _bytePanelContainer.Visible = true;
            }
        }

        /// <summary>
        /// Clears the container's current item.
        /// </summary>
        public void ClearItem()
        {
            DebugLogger.LogMessage($"Clearing item...", true);
            _item = null;
            _textureRect.Texture = null;
        }

        private void ClearPriceLabels()
        {
            _fluxPanelContainer.Visible = false;
            _bytePanelContainer.Visible = false;
        }

        /// <summary>
        /// Called when the player presses the "ui_select" action while this ShopItemContainer is in focus.
        /// </summary>
        public void ItemSelected()
        {
            if (_item != null)
            {
                ShopItemSelected?.Invoke(this, new ShopItemSelectedEventArgs(_item));
            }
        }

        public void ItemBought()
        {
            DebugLogger.LogMessage($"Item bought called!", true);
            _itemNameLabel.Text += " Bought!";
            ClearItem();
        }

        private void OnFocusEnter()
        {
            // DebugLogger.LogMessage(
            //     $"{Name} focus entered! Changing border color to _itemColor: {_itemColor}",
            //     true
            // );
            _focusedStyleBox.BorderColor = _itemColor;
            AddThemeStyleboxOverride("panel", _focusedStyleBox);
        }

        private void OnFocusExit()
        {
            // DebugLogger.LogMessage($"{Name} focus exited!", true);
            AddThemeStyleboxOverride("panel", _defocusedStyleBox);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
