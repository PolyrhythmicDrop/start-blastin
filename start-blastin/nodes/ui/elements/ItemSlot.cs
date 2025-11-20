using System;
using System.Diagnostics;
using Godot;
using Interfaces;
using Items;
using Utility;

[GlobalClass]
public partial class ItemSlot : PanelContainer, IListener
{
    protected Item _item;
    protected TextureRect _frame;
    protected TextureRect _mask;
    protected TextureRect _iconRect;

    public Item Item => _item;
    public bool Empty => _item == null;

    protected event Action SlotItemChanged;

    public override void _Ready()
    {
        DebugLogger.LogMessage($"Calling ready...", true);
        _iconRect = GetNode<TextureRect>("%IconRect");

        if (_item != null && _iconRect != null)
        {
            UpdateIconRect();
        }

        SlotItemChanged += UpdateIconRect;
    }

    public virtual void ConnectSignals()
    {
        SlotItemChanged += UpdateIconRect;
    }

    public virtual void DisconnectSignals()
    {
        SlotItemChanged -= UpdateIconRect;
    }

    public virtual void SetItem(Item item)
    {
        DebugLogger.LogMessage($"Setting item: {item}", true);
        _item = item;
        SlotItemChanged?.Invoke();
    }

    public virtual void ClearItem()
    {
        _item = null;
    }

    public virtual void UpdateIconRect()
    {
        if (_iconRect != null && _item != null)
        {
            _iconRect.Texture = _item.Icon;
        }
        else
        {
            return;
        }
    }

    public virtual void ClearIconRect()
    {
        if (_iconRect != null)
        {
            _iconRect.Texture = null;
        }
    }
}
