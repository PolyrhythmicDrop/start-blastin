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
        SetLabelSettings();
    }

    public void SetLabelSettings()
    {
        _label.LabelSettings = _labelSettings switch
        {
            ItemNameLabelSettings.Shop => _shopLabelSettings,
            ItemNameLabelSettings.Inventory => _inventoryLabelSettings,
            ItemNameLabelSettings.Default => _defaultLabelSettings,
            _ => _defaultLabelSettings,
        };
    }
}
