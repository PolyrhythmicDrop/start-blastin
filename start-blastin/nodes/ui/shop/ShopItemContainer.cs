using System;
using Events;
using Godot;
using Items;
using Utility;

namespace UI.Shop
{
    [GlobalClass]
    public partial class ShopItemContainer : ItemContainer
    {
        // ~ Nodes ~ //
        private PanelContainer _bytePanelContainer;
        private PanelContainer _fluxPanelContainer;
        private Label _byteLabel;
        private Label _fluxLabel;

        // private Color _itemColor;
        private Color _defaultPriceColor = new Color("#ffffff");
        private Color _unbuyablePriceColor = new Color("#ff5470");

        // public Item Item => _item;

        public event EventHandler<ShopItemSelectedEventArgs> ShopItemSelected;

        public override void _Ready()
        {
            base._Ready();
            _bytePanelContainer = GetNode<PanelContainer>("%BytePanelContainer");
            _fluxPanelContainer = GetNode<PanelContainer>("%FluxPanelContainer");
            _byteLabel = GetNode<Label>("%ByteLabel");
            _fluxLabel = GetNode<Label>("%FluxLabel");
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

        /// <summary>
        /// Sets the item that belongs in the container.
        /// </summary>
        /// <param name="item">The item to place in the container.</param>
        public override void SetItem(Item item)
        {
            base.SetItem(item);

            SetItemPriceLabels();
        }

        /// <summary>
        /// Sets the price label text and visibility based on the ShopItemContainer's loaded item.
        /// </summary>
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

        /// <summary>
        /// Sets the color of the price labels according to whether or not the player has enough currency to purchase it.
        /// </summary>
        /// <param name="fluxBuyable">True if the player has enough flux to buy the item.</param>
        /// <param name="byteBuyable">True if the player has enough bytes to buy the item.</param>
        /// <remarks>
        /// Called from the ShopUI object that manages this ShopItemContainer.
        /// </remarks>
        public void SetBuyable(bool fluxBuyable, bool byteBuyable)
        {
            if (fluxBuyable)
            {
                _fluxLabel.LabelSettings.FontColor = _defaultPriceColor;
            }
            else
            {
                _fluxLabel.LabelSettings.FontColor = _unbuyablePriceColor;
            }

            if (byteBuyable)
            {
                _byteLabel.LabelSettings.FontColor = _defaultPriceColor;
            }
            else
            {
                _byteLabel.LabelSettings.FontColor = _unbuyablePriceColor;
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
        }
    }
}
