using System;
using Godot;
using Interfaces;
using Items;
using Utility;

[GlobalClass]
public partial class ItemDisplay : PanelContainer, IListener
{
    private Texture2D _pluginBorder => GD.Load<Texture2D>("uid://d2b3ue4ow24pb");
    private Texture2D _pluginMask => GD.Load<Texture2D>("uid://drp3a6v1mvedl");
    private Texture2D _weaponBorder => GD.Load<Texture2D>("uid://djbojubn7plf");
    private Texture2D _weaponMask => GD.Load<Texture2D>("uid://bbk7wfmggd6wr");

    protected Item _item;
    protected TextureRect _border;
    protected TextureRect _mask;
    protected TextureRect _iconRect;

    public Item Item => _item;
    public bool Empty => IsEmpty();

    public event Action SlotItemChanged;

    public override void _Ready()
    {
        _iconRect = GetNode<TextureRect>("%IconRect");
        _border = GetNode<TextureRect>("%BorderRect");
        _mask = GetNode<TextureRect>("%MaskRect");

        if (_iconRect != null)
        {
            UpdateIconRects();
        }

        ConnectSignals();
    }

    public virtual void ConnectSignals()
    {
        SlotItemChanged += UpdateIconRects;
    }

    public virtual void DisconnectSignals()
    {
        SlotItemChanged -= UpdateIconRects;
    }

    public virtual void SetItem(Item item)
    {
        _item = item;
        SlotItemChanged?.Invoke();
    }

    private void SetBorder()
    {
        if (_border != null && _mask != null)
        {
            switch (_item)
            {
                case WeaponPlugin:
                    _border.Texture = _weaponBorder;
                    _mask.Texture = _weaponMask;
                    break;
                case Plugin:
                    _border.Texture = _pluginBorder;
                    _mask.Texture = _pluginMask;
                    break;
                case null:
                default:
                    _border.Texture = null;
                    _mask.Texture = null;
                    break;
            }
        }
    }

    public virtual void ClearItem()
    {
        _item = null;
        SlotItemChanged?.Invoke();
    }

    public virtual void UpdateIconRects()
    {
        if (_iconRect != null)
        {
            _iconRect.Texture = _item?.Icon;
            SetBorder();
        }
    }

    public virtual void ClearIconRect()
    {
        if (_iconRect != null)
        {
            _iconRect.Texture = null;
        }
    }

    private bool IsEmpty()
    {
        bool itemNull = _item == null;
        bool hasBlankPlugin = _item == ResourceLoader.Load<Plugin>("uid://cdf365jvnlftb");
        return itemNull || hasBlankPlugin;
    }
}
