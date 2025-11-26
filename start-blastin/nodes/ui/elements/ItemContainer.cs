using System;
using Events;
using Godot;
using Interfaces;
using Items;
using Utility;

namespace UI
{
    [GlobalClass]
    public partial class ItemContainer : PanelContainer, IListener
    {
        // protected Item _item;

        // protected TextureRect _itemIconRect;
        protected ItemDisplay _itemDisplay;

        protected TextureRect _impossibleActionRect;
        protected PackedScene _impossibleScene => GD.Load<PackedScene>("uid://beguupgtrbesp");

        protected ItemNamePanelContainer _itemNamePanel;
        protected StyleBoxFlat _defocusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>("uid://bluwbrc16b4ns");
        protected StyleBoxFlat _focusedStyleBox =>
            ResourceLoader.Load<StyleBoxFlat>("uid://chnsppbtk2va0");

        protected Color _itemColor;

        public ItemDisplay ItemDisplay => _itemDisplay;
        public Item Item => _itemDisplay?.Item;

        public event EventHandler<ItemSelectedEventArgs> ItemContainerSelected;

        public override void _Ready()
        {
            _itemDisplay = GetNode<ItemDisplay>("%ItemDisplay");
            _impossibleActionRect = _impossibleScene.Instantiate<TextureRect>();
            _itemDisplay.AddChild(_impossibleActionRect);

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
            if (Item != null)
            {
                ClearItem();
            }

            // Set the item in the item display.
            // Sets the display's texture and border
            // Also sets the ItemContainer.Item variable, since that pulls from the item in the display
            _itemDisplay.SetItem(item);

            // TODO: Values are hardcoded here, but we could probably make them constants for item rarity.
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
            _itemNamePanel.Label.Text = Item.Name;
        }

        /// <summary>
        /// Clears the container's current item.
        /// </summary>
        public void ClearItem()
        {
            _itemDisplay.ClearItem();
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
            if (Item != null)
            {
                ItemContainerSelected?.Invoke(this, new ItemSelectedEventArgs(Item));
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
