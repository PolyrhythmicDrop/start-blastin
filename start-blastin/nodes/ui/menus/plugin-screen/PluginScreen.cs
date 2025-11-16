using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Godot;
using Interfaces;
using Services;
using UI.HUD;
using Utility;

public partial class PluginScreen : PanelContainer, IListener
{
    private int _playerId;
    private PlayerService _service => ServiceManager.Instance.GetService<PlayerService>();
    private LoadoutPanel _loadoutPanel;
    private MarginContainer _marginContainer;
    private WeaponSlot _weaponSlot => _loadoutPanel.WeapSlot;
    private IReadOnlyList<PluginSlot> _pluginSlots => _loadoutPanel.PluginSlots;
    private DescriptionPanel _descriptionPanel;

    private Dictionary<PluginSlot, Action> _slotFocusEnteredActions = new();
    private Dictionary<PluginSlot, Action> _slotFocusExitedActions = new();

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
        _loadoutPanel = GD.Load<PackedScene>("uid://b3t5it47lwtr1").Instantiate<LoadoutPanel>();
        _marginContainer = GetNode<MarginContainer>("%MarginContainer");
        _marginContainer.AddChild(_loadoutPanel);
        _descriptionPanel = GetNode<DescriptionPanel>("%DescriptionPanelContainer");

        if (!_loadoutPanel.Initialized)
        {
            _loadoutPanel.Initialize(_playerId);
            _loadoutPanel.HBox.ThemeTypeVariation = "PluginScreenLoadoutHBox";
            BuildPluginScreen();
        }

        SetFocusModes();
        AssignNeighbors();
        ConnectSignals();
        _weaponSlot.GrabFocus();
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
            slot.Reparent(vBox);
        }
    }

    private void SetFocusModes()
    {
        _weaponSlot.SetFocusMode(FocusModeEnum.All);
        foreach (PluginSlot slot in _pluginSlots)
        {
            slot.SetFocusMode(FocusModeEnum.All);
        }
    }

    private void AssignNeighbors()
    {
        int count = _pluginSlots.Count;
        // Assign the weapon slot's neighbors.
        _weaponSlot.FocusNeighborLeft = _pluginSlots[count - 1].GetPath();
        _weaponSlot.FocusNeighborRight = _pluginSlots[0].GetPath();

        // Assign the rest of the plugin slots
        for (int i = 0; i < _pluginSlots.Count; i++)
        {
            // Set the left-most slot
            if (i == 0)
            {
                _pluginSlots[i].FocusNeighborLeft = _weaponSlot.GetPath();
                _pluginSlots[i].FocusNeighborRight = _pluginSlots[i + 1].GetPath();
            }
            // Set the right-most slot
            else if (i == _pluginSlots.Count - 1)
            {
                _pluginSlots[i].FocusNeighborRight = _weaponSlot.GetPath();
                _pluginSlots[i].FocusNeighborLeft = _pluginSlots[i - 1].GetPath();
            }
            // Set the middle slots
            else
            {
                _pluginSlots[i].FocusNeighborRight = _pluginSlots[i + 1].GetPath();
                _pluginSlots[i].FocusNeighborLeft = _pluginSlots[i - 1].GetPath();
            }
        }
    }

    public void ConnectSignals()
    {
        // Connect the weapon slot
        _weaponSlot.FocusEntered += OnWeaponSlotFocusEntered;
        _weaponSlot.FocusExited += OnWeaponSlotFocusExited;
        // Connect the plugin slots to the item description display and the other focus methods.
        foreach (PluginSlot slot in _pluginSlots)
        {
            PluginSlot captured = slot;
            Action enteredHandler = () =>
            {
                OnSlotFocusEntered(captured);
            };
            _slotFocusEnteredActions[captured] = enteredHandler;
            captured.FocusEntered += enteredHandler;

            Action exitedHandler = () =>
            {
                OnSlotFocusExited(captured);
            };
            _slotFocusExitedActions[captured] = exitedHandler;
            captured.FocusExited += exitedHandler;
        }
    }

    public void DisconnectSignals()
    {
        _weaponSlot.FocusEntered -= OnWeaponSlotFocusEntered;
        _weaponSlot.FocusExited -= OnWeaponSlotFocusExited;

        foreach (KeyValuePair<PluginSlot, Action> kvp in _slotFocusEnteredActions)
        {
            kvp.Key.FocusEntered -= kvp.Value;
        }
        foreach (KeyValuePair<PluginSlot, Action> kvp in _slotFocusExitedActions)
        {
            kvp.Key.FocusExited -= kvp.Value;
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
                namePanel.Label.Text = "Empty";
            }
        }
    }

    private void OnWeaponSlotFocusEntered() => OnSlotFocusEntered(_weaponSlot);

    private void OnSlotFocusEntered(PluginSlot slot)
    {
        DebugLogger.LogMessage($"{slot.Name} focus entered!", true);
    }

    private void OnWeaponSlotFocusExited() => OnSlotFocusExited(_weaponSlot);

    private void OnSlotFocusExited(PluginSlot slot)
    {
        DebugLogger.LogMessage($"{slot.Name} focus exited!", true);
    }

    public override void _ExitTree()
    {
        Active = false;
        DisconnectSignals();
        _loadoutPanel.QueueFree();
        base._ExitTree();
    }
}
