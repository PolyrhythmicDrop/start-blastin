using System;
using Godot;

public enum ItemNameLabelSettings
{
    Default,
    Shop,
    Inventory,
}

[GlobalClass]
public partial class ItemNamePanelContainer : PanelContainer
{
    private Label _label;
    private LabelSettings _defaultLabelSettings => GD.Load<LabelSettings>("uid://dhmvqno5lqhj");
    private LabelSettings _shopLabelSettings => GD.Load<LabelSettings>("uid://bxecrmsi8u1b5");
    private LabelSettings _inventoryLabelSettings => GD.Load<LabelSettings>("uid://cwxulm5ukxovo");
    private StyleBoxFlat _shopStyleBox = GD.Load<StyleBoxFlat>("uid://bfhwy5ee28np2");
    private StyleBoxFlat _inventoryStyleBox = GD.Load<StyleBoxFlat>("uid://cetbqp86dtgei");
    private ItemNameLabelSettings _labelSettings = ItemNameLabelSettings.Default;

    public Label Label => _label;

    [Export]
    public ItemNameLabelSettings NameLabelSettings
    {
        get => _labelSettings;
        set => _labelSettings = value;
    }

    public override void _Ready()
    {
        _label = GetNode<Label>("%ItemNameLabel");
        SetStyle();
    }

    public void SetStyle()
    {
        StyleBoxFlat box;
        switch (_labelSettings)
        {
            default:
            case ItemNameLabelSettings.Default:
                _label.LabelSettings = _defaultLabelSettings;
                box = _shopStyleBox;
                break;
            case ItemNameLabelSettings.Shop:
                _label.LabelSettings = _shopLabelSettings;
                box = _shopStyleBox;
                break;
            case ItemNameLabelSettings.Inventory:
                _label.LabelSettings = _inventoryLabelSettings;
                box = _inventoryStyleBox;
                break;
        }
        AddThemeStyleboxOverride("panel", box);
    }
}
