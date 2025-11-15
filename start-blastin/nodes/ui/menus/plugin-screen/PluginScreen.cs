using System.Collections.Generic;
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

    public void BuildPluginScreen() { }

    private void AddLabelToSlot(PluginSlot slot) { }

    public override void _ExitTree()
    {
        Active = false;
        base._ExitTree();
    }
}
