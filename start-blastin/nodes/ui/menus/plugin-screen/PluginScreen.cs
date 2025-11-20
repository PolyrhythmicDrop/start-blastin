using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using Interfaces;
using Items;
using Services;
using UI.HUD;
using Utility;

namespace UI.Loadout
{
    public partial class PluginScreen : PanelContainer
    {
        private int _playerId;

        private LoadoutDisplay _loadoutDisplay;
        private MarginContainer _pluginMargins;
        private HBoxContainer _pluginHBox;

        private List<InventoryItemContainer> _pluginContainers = new();
        private InventoryItemContainer _weaponContainer;
        private DescriptionPanel _descriptionPanel;

        private PackedScene _inventoryItemContainerScene = GD.Load<PackedScene>(
            "uid://cx50s2c1i7ysb"
        );

        private Color _sellPriceColor;

        [Export]
        public Color SellPriceColor
        {
            get => _sellPriceColor;
            set => _sellPriceColor = value;
        }

        public bool Active;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            Visible = false;
            _loadoutDisplay = new();
            _loadoutDisplay.Initialize(UiLayer.GetUiLayer(_playerId).LoadoutManager);
        }

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Starting _Ready...", true);
            _pluginMargins = GetNode<MarginContainer>("%PluginMargins");
            _pluginHBox = GetNode<HBoxContainer>("%PluginHBox");

            // _pluginMargins.AddChild(_loadoutPanel);
            _descriptionPanel = GetNode<DescriptionPanel>("%DescriptionPanelContainer");

            BuildPluginScreen();
            SetFocusModes();
            AssignNeighbors();
        }

        public void ToggleActivate(bool activate)
        {
            if (activate)
            {
                Active = true;
                Visible = true;
                _weaponContainer.CallDeferred(MethodName.GrabFocus);
            }
            else
            {
                Visible = false;
                Active = false;
            }
        }

        public void BuildPluginScreen()
        {
            // New implementation
            DebugLogger.LogMessage($"Building plugin screen...", true);

            // Create the container for the weapon slot
            WrapWeaponSlot();

            DebugLogger.LogMessage($"Wrapping the loadout's plugin slots...", true);

            // Create containers for the rest of the equipped plugins
            foreach (PluginSlot slot in _loadoutDisplay.PluginSlots)
            {
                WrapPluginSlot(slot);
            }
        }

        private void WrapWeaponSlot()
        {
            DebugLogger.LogMessage($"Wrapping the loadout's weapon slot...", true);
            InventoryItemContainer weapContainer =
                _inventoryItemContainerScene.Instantiate<InventoryItemContainer>();
            _weaponContainer = weapContainer;
            DebugLogger.LogMessage($"Adding the new container as a child to the HBox...", true);
            _pluginHBox.AddChild(_weaponContainer);
            _weaponContainer.SetItemSlot(_loadoutDisplay.WeapSlot);
            _weaponContainer.Slot.PivotOffset = _loadoutDisplay.WeapSlot.Size / 2;
            _weaponContainer.Slot.Scale = new Vector2(0.8f, 0.8f);
        }

        private void WrapPluginSlot(PluginSlot slot)
        {
            DebugLogger.LogMessage($"Wrapping the plugin slot {slot.Name}", true);
            InventoryItemContainer itemContainer =
                _inventoryItemContainerScene.Instantiate<InventoryItemContainer>();
            _pluginHBox.AddChild(itemContainer);
            itemContainer.SetItemSlot(slot);
            slot.PivotOffset = slot.Size / 2;
            slot.Scale = new Vector2(0.8f, 0.8f);
            _pluginContainers.Add(itemContainer);
        }

        private void SetFocusModes()
        {
            DebugLogger.LogMessage($"Setting focus modes...", true);
            _weaponContainer.SetFocusMode(FocusModeEnum.All);
            foreach (InventoryItemContainer container in _pluginContainers)
            {
                container.SetFocusMode(FocusModeEnum.All);
            }
        }

        private void AssignNeighbors()
        {
            int count = _pluginContainers.Count;
            // Assign the weapon slot's neighbors.
            if (_weaponContainer != null)
            {
                _weaponContainer.FocusNeighborLeft = _pluginContainers[count - 1].GetPath();
                _weaponContainer.FocusNeighborRight = _pluginContainers[0].GetPath();
            }

            // Assign the rest of the plugin slots
            for (int i = 0; i < _pluginContainers.Count; i++)
            {
                // Set the left-most slot
                if (i == 0)
                {
                    _pluginContainers[i].FocusNeighborLeft = _weaponContainer.GetPath();
                    _pluginContainers[i].FocusNeighborRight = _pluginContainers[i + 1].GetPath();
                }
                // Set the right-most slot
                else if (i == _pluginContainers.Count - 1)
                {
                    _pluginContainers[i].FocusNeighborRight = _weaponContainer.GetPath();
                    _pluginContainers[i].FocusNeighborLeft = _pluginContainers[i - 1].GetPath();
                }
                // Set the middle slots
                else
                {
                    _pluginContainers[i].FocusNeighborRight = _pluginContainers[i + 1].GetPath();
                    _pluginContainers[i].FocusNeighborLeft = _pluginContainers[i - 1].GetPath();
                }
            }
        }

        public override void _ExitTree()
        {
            Active = false;
            base._ExitTree();
        }
    }
}
