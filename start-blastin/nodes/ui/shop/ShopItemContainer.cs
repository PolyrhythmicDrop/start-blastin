using System;
using Godot;
using Items;

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
    }

    public void AddItem(Item item)
    {
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

        GD.Print(
            $"Item settings configured! Item: {_item.Name} | Texture on TextureRect: {_textureRect.Texture} | Text Color: {_rtLabel.GetThemeColor("default_color")}"
        );
    }

    public void ClearItem()
    {
        _item = null;
    }
}
