using System;
using Godot;
using Items;

[GlobalClass]
public partial class ItemSlot : PanelContainer
{
    protected Item _item;
    protected TextureRect _frame;
    protected TextureRect _mask;
    protected TextureRect _icon;

    public Item Item => _item;

    public virtual void SetItem(Item item)
    {
        _item = item;
        _icon.Texture = _item.Icon;
    }

    public virtual void ClearItem()
    {
        _item = null;
        _icon.Texture = null;
    }
}
