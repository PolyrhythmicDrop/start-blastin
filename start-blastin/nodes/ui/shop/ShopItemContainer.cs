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
        private PricePanelContainer _pricePanel;

        private Color _defaultPriceColor = new Color("#ffffff");
        private Color _unbuyablePriceColor = new Color("#ff5470");

        public override void _Ready()
        {
            base._Ready();
            _pricePanel = GetNode<PricePanelContainer>("%PricePanelContainer");
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
            // int flux = _item.FluxCost;
            // int bytes = _item.ByteCost;

            int flux = Item.FluxCost;
            int bytes = Item.ByteCost;

            _pricePanel.SetLabelText(bytes.ToString("N0"), PricePanelContainer.PriceLabel.Bytes);
            _pricePanel.SetLabelText(flux.ToString("N0"), PricePanelContainer.PriceLabel.Flux);

            bool costsBytes = bytes > 0;
            bool costsFlux = flux > 0;
            _pricePanel.TogglePanelVisibility(costsBytes, PricePanelContainer.PriceLabel.Bytes);
            _pricePanel.TogglePanelVisibility(costsFlux, PricePanelContainer.PriceLabel.Flux);
        }

        private void ClearPriceLabels()
        {
            _pricePanel.TogglePanelVisibility(false);
        }

        public void ItemBought()
        {
            _itemNamePanel.Label.Text += " Bought!";
            ClearItem();
        }

        /// <summary>
        /// Sets various aspects of the item container according to whether or not the player has enough currency to purchase it.
        /// </summary>
        /// <param name="fluxBuyable">True if the player has enough flux to buy the item.</param>
        /// <param name="byteBuyable">True if the player has enough bytes to buy the item.</param>
        /// <param name="canBuy">True if the player can buy the item overall</param>
        /// <remarks>
        /// Called from the ShopUI object that manages this ShopItemContainer.
        /// </remarks>
        public void SetBuyable(bool fluxBuyable, bool byteBuyable, bool canBuy)
        {
            Color fluxColor = fluxBuyable ? _defaultPriceColor : _unbuyablePriceColor;
            Color byteColor = byteBuyable ? _defaultPriceColor : _unbuyablePriceColor;

            _pricePanel.SetFontColor(byteColor, PricePanelContainer.PriceLabel.Bytes);
            _pricePanel.SetFontColor(fluxColor, PricePanelContainer.PriceLabel.Flux);

            if (!canBuy)
            {
                _impossibleActionRect.Visible = true;
            }
            else
            {
                _impossibleActionRect.Visible = false;
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
        }
    }
}
