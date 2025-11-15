using System.Collections.Generic;
using System.Linq;
using Entities;
using Godot;
using Services;
using UI.HUD;

public partial class PluginScreen : PanelContainer
{
    private int _playerId;
    private PlayerService _service => ServiceManager.Instance.GetService<PlayerService>();
    private LoadoutPanel _loadoutPanel;
    private WeaponSlot _weaponSlot => _loadoutPanel.WeapSlot;
    private IReadOnlyList<PluginSlot> _pluginSlots => _loadoutPanel.PluginSlots;

    private PackedScene _itemNameScene = GD.Load<PackedScene>("uid://cwxfvdq5l7brl");

    public int PlayerId => _playerId;

    public bool Active;

    public void Initialize(int playerId)
    {
        _playerId = playerId;
        Visible = false;
    }

    public override void _Ready()
    {
        _loadoutPanel = GetNode<LoadoutPanel>("%LoadoutPanel");
        if (!_loadoutPanel.Initialized)
        {
            _loadoutPanel.Initialize(_playerId);
            _loadoutPanel.HBox.ThemeTypeVariation = "PluginScreenLoadoutHBox";
        }
        Active = true;
    }

    public void BuildPluginScreen()
    {
        AddVBoxes();
        foreach (PluginSlot slot in _pluginSlots)
        {
            AddLabelToSlot(slot);
        }
        AddLabelToSlot(_weaponSlot);
    }

    private void AddVBoxes()
    {
        var hBoxChildren = _loadoutPanel.HBox.GetChildren();
        foreach (PluginSlot slot in hBoxChildren)
        {
            VBoxContainer vBox = new VBoxContainer();
            _loadoutPanel.HBox.AddChild(vBox);
            // int index = hBoxChildren.IndexOf(vBox);
            // _loadoutPanel.HBox.MoveChild(slot, index);
            slot.Reparent(vBox);
        }
    }

    private void AddLabelToSlot(PluginSlot slot)
    {
        if (slot.GetParent() is VBoxContainer vbox)
        {
            ItemNamePanelContainer namePanel = _itemNameScene.Instantiate<ItemNamePanelContainer>();
            namePanel.NameLabelSettings = ItemNameLabelSettings.Inventory;
            vbox.AddChild(namePanel);
            vbox.MoveChild(namePanel, 0);
            namePanel.SetLabelSettings();
            if (slot.Plugin != null)
            {
                namePanel.Label.Text = slot.Plugin.Name;
            }
            else
            {
                // namePanel.Visible = false;
                namePanel.Label.Text = "Empty";
            }
        }
    }

    public override void _ExitTree()
    {
        Active = false;
        base._ExitTree();
    }
}
