using System;
using System.Diagnostics;
using Autoloads;
using Godot;
using Items;
using Utility;

[GlobalClass]
public partial class ShopItemContainer : Control
{
    private Item _item;
    private TextureRect _textureRect;
    private RichTextLabel _rtLabel;

    public Item Item => _item;

    public override void _Ready()
    {
        GD.Print($"{Name} ready called!");
        _textureRect = GetNode<TextureRect>("%ItemIcon");
        _rtLabel = GetNode<RichTextLabel>("%RTLabel");
        FocusEntered += OnFocused;
    }

    public override void _GuiInput(InputEvent @event)
    {
        // DebugLogger.LogMessage($"Event {@event} detected!", true);

        if (@event.IsAction("ui_accept"))
        {
            DebugLogger.LogMessage($"Action event 'ui_accept' detected!", true);
            ItemSelected();
            AcceptEvent();
        }
    }

    public override void _Process(double delta) { }

    public void SetItem(Item item)
    {
        if (_item != null)
        {
            ClearItem();
        }

        _item = item;
        Color fontColor = new("FLORAL_WHITE");

        switch (item.Rarity)
        {
            case Rarity.Common:
            default:
                break;
            case Rarity.Uncommon:
                fontColor = new("#78d8b7");
                break;
            case Rarity.Rare:
                fontColor = new("#fdfe89");
                break;
            case Rarity.Legendary:
                fontColor = new("#ff5470");
                break;
        }

        _rtLabel.AddThemeColorOverride("default_color", fontColor);
        _rtLabel.Text = _item.Name;

        _textureRect.Texture = _item.Icon;
    }

    public void ClearItem()
    {
        GD.Print($"Clearing item...");
        _item = null;
        _textureRect.Texture = null;
        _rtLabel.Text += " Bought!";
    }

    public void ItemSelected()
    {
        DebugLogger.LogMessage($"Shop item {_item} bought!", true);
        // Buy the item
        // TODO: display the item's description and stuff before buying it. This is just to test that I *can* buy it.
        EventBus.Instance.EmitSignal(EventBus.SignalName.ShopItemBought, _item);

        // Clear the item.
        ClearItem();
    }

    private void OnFocused()
    {
        // DebugLogger.LogMessage($"{Name} is now in focus!", true);
    }
}
