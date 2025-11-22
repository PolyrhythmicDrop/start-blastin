using System;
using Events;
using Godot;
using Interfaces;
using Items;
using Utility;

namespace UI
{
    public enum ItemType
    {
        Mod,
        Plugin,
        Weapon,
        Consumable,
    }

    [GlobalClass]
    public partial class ItemContainer : PanelContainer, IListener
    {
        protected Item _item;
        protected TextureRect _textureRect;

        protected ItemNamePanelContainer _itemNamePanel;
        protected StyleBoxFlat _defocusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>("uid://bluwbrc16b4ns");
        protected StyleBoxFlat _focusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>("uid://chnsppbtk2va0");

        protected Color _itemColor;

        public Item Item => _item;

        public event EventHandler<ItemSelectedEventArgs> ItemContainerSelected;

        public override void _Ready()
        {
            _textureRect = GetNode<TextureRect>("%ItemIcon");
            // _itemNameLabel = GetNode<Label>("%ItemNameLabel");
            _itemNamePanel = GetNode<ItemNamePanelContainer>("%ItemNamePanelContainer");
            ConnectSignals();
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (Input.IsActionJustPressedByEvent("ui_accept", @event))
            {
                DebugLogger.LogMessage($"Gui input ui_accept detected by {Name}!");
                InvokeItemContainerSelected();
                AcceptEvent();
            }
        }

        public virtual void ConnectSignals()
        {
            FocusEntered += OnFocusEnter;
            FocusExited += OnFocusExit;
        }

        public virtual void DisconnectSignals()
        {
            FocusEntered -= OnFocusEnter;
            FocusExited -= OnFocusExit;
        }

        public virtual void SetItem(Item item)
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

            _itemNamePanel.Label.LabelSettings.FontColor = _itemColor;
            _itemNamePanel.Label.Text = _item.Name;

            _textureRect.Texture = _item.Icon;
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

        protected virtual void OnFocusEnter()
        {
            _focusedStyleBox.BorderColor = _itemColor;
            AddThemeStyleboxOverride("panel", _focusedStyleBox);
        }

        protected virtual void OnFocusExit()
        {
            AddThemeStyleboxOverride("panel", _defocusedStyleBox);
        }

        public virtual void InvokeItemContainerSelected()
        {
            DebugLogger.LogMessage(
                $"Attempting to invoke ItemContainerSelected. Is {nameof(_item)} null? {_item == null}",
                true
            );
            if (_item != null)
            {
                DebugLogger.LogMessage($"Invoking ItemContainerSelected...", true);
                ItemContainerSelected?.Invoke(this, new ItemSelectedEventArgs(_item));
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
