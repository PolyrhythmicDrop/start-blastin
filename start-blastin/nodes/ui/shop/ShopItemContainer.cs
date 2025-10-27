using System;
using System.Diagnostics;
using Autoloads;
using Godot;
using Items;
using Utility;

namespace Shop
{
    [GlobalClass]
    public partial class ShopItemContainer : Control
    {
        private Item _item;
        private TextureRect _textureRect;
        private RichTextLabel _rtLabel;
        private PanelContainer _panelContainer;
        private StyleBoxFlat _defocusedStyleBox;
        private StyleBoxFlat _focusedStyleBox;
        private Color _itemColor;
        public Item Item => _item;

        public override void _Ready()
        {
            GD.Print($"{Name} ready called!");
            _textureRect = GetNode<TextureRect>("%ItemIcon");
            _rtLabel = GetNode<RichTextLabel>("%RTLabel");
            _panelContainer = GetNode<PanelContainer>("%PanelContainer");
            _focusedStyleBox = ResourceLoader.Load<StyleBoxFlat>(
                "res://resources/themes/styleboxes/item-container-focused-stylebox.tres"
            );
            _defocusedStyleBox = ResourceLoader.Load<StyleBoxFlat>(
                "res://resources/themes/styleboxes/item-container-defocused-stylebox.tres"
            );
            FocusEntered += OnFocusEnter;
            FocusExited += OnFocusExit;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event.IsAction("ui_accept"))
            {
                ItemSelected();
                AcceptEvent();
            }
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

            _rtLabel.AddThemeColorOverride("default_color", _itemColor);
            _rtLabel.Text = _item.Name;

            _textureRect.Texture = _item.Icon;
        }

        /// <summary>
        /// Clears the container's current item.
        /// </summary>
        public void ClearItem()
        {
            GD.Print($"Clearing item...");
            _item = null;
            _textureRect.Texture = null;
        }

        /// <summary>
        /// Called when the player presses the "ui_select" action while this ShopItemContainer is in focus.
        /// </summary>
        public void ItemSelected()
        {
            if (_item != null)
            {
                DebugLogger.LogMessage($"Shop item {_item.Name} bought!", true);
                // Buy the item
                // TODO: display the item's description and stuff before buying it. This is just to test that I *can* buy it.
                EventBus.Instance.EmitSignal(EventBus.SignalName.ShopItemBought, _item);
                _rtLabel.Text += " Bought!";

                // Clear the item.
                ClearItem();
            }
        }

        private void OnFocusEnter()
        {
            _focusedStyleBox.BorderColor = _itemColor;
            _panelContainer.AddThemeStyleboxOverride("panel", _focusedStyleBox);
        }

        private void OnFocusExit()
        {
            _panelContainer.AddThemeStyleboxOverride("panel", _defocusedStyleBox);
        }
    }
}
