using System;
using System.Collections.Generic;
using Autoloads;
using Events;
using Godot;
using Interfaces;
using Items;
using Services;
using UI.HUD;
using Utility;

namespace UI.Loadout
{
    public partial class PluginScreen : PanelContainer, IListener
    {
        private int _playerId;

        private LoadoutDisplay _loadoutDisplay;
        private MarginContainer _pluginMargins;
        private HBoxContainer _pluginHBox;

        private List<InventoryItemContainer> _pluginContainers = new();
        private Dictionary<InventoryItemContainer, Action> _containerFocusEnteredCallbacks = new();
        private Dictionary<InventoryItemContainer, Action> _containerFocusExitedCallbacks = new();

        private InventoryItemContainer _weaponContainer;
        private DescriptionPanel _descriptionPanel;

        private PackedScene _inventoryItemContainerScene = GD.Load<PackedScene>(
            "uid://cx50s2c1i7ysb"
        );

        public bool Active;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            Visible = false;
            _loadoutDisplay = new();
            _loadoutDisplay.Initialize(_playerId);
        }

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Starting Plugin Screen _Ready...", true);
            _pluginMargins = GetNode<MarginContainer>("%PluginMargins");
            _pluginHBox = GetNode<HBoxContainer>("%PluginHBox");

            _descriptionPanel = GetNode<DescriptionPanel>("%DescriptionPanelContainer");

            BuildPluginScreen();
            SetFocusModes();
            AssignNeighbors();
            ConnectSignals();
        }

        public void ConnectSignals()
        {
            _weaponContainer.FocusEntered += OnWeaponContainerFocusEntered;

            foreach (InventoryItemContainer container in _pluginContainers)
            {
                // Capture the containers to assign Action-based callbacks.
                InventoryItemContainer capturedCont = container;
                Action enteredHandler = () =>
                {
                    OnContainerFocusEntered(capturedCont);
                };
                Action exitedHandler = () =>
                {
                    OnContainerFocusExited(capturedCont);
                };
                _containerFocusEnteredCallbacks[capturedCont] = enteredHandler;
                _containerFocusExitedCallbacks[capturedCont] = exitedHandler;
                capturedCont.FocusEntered += enteredHandler;
                capturedCont.FocusExited += exitedHandler;

                // Connect item selected callbacks
                container.ItemContainerSelected += OnItemContainerSelected;
            }

            // Connect display updated
            _loadoutDisplay.DisplayUpdated += OnDisplayUpdated;
        }

        public void DisconnectSignals()
        {
            _weaponContainer.FocusEntered -= OnWeaponContainerFocusEntered;

            foreach (
                KeyValuePair<InventoryItemContainer, Action> kvp in _containerFocusEnteredCallbacks
            )
            {
                kvp.Key.FocusEntered -= kvp.Value;
            }

            foreach (
                KeyValuePair<InventoryItemContainer, Action> kvp in _containerFocusExitedCallbacks
            )
            {
                kvp.Key.FocusExited -= kvp.Value;
            }

            // Disconnect item selected callbacks
            foreach (InventoryItemContainer container in _pluginContainers)
            {
                container.ItemContainerSelected -= OnItemContainerSelected;
            }
            _loadoutDisplay.DisplayUpdated -= OnDisplayUpdated;
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
            // Create the container for the weapon slot
            WrapWeaponSlot();
            // Create containers for the rest of the equipped plugins
            foreach (ItemDisplay display in _loadoutDisplay.PluginDisplays)
            {
                WrapPluginSlot(display);
            }
        }

        private void WrapWeaponSlot()
        {
            DebugLogger.LogMessage($"Wrapping the loadout's weapon slot...", true);
            InventoryItemContainer weapContainer =
                _inventoryItemContainerScene.Instantiate<InventoryItemContainer>();
            _weaponContainer = weapContainer;
            _pluginHBox.AddChild(_weaponContainer);
            _weaponContainer.SetItemDisplay(_loadoutDisplay.WeapSlot);
            _weaponContainer.ItemDisplay.PivotOffset = _loadoutDisplay.WeapSlot.Size / 2;
            _weaponContainer.ItemDisplay.Scale = new Vector2(0.8f, 0.8f);
        }

        private void WrapPluginSlot(ItemDisplay display)
        {
            // DebugLogger.LogMessage($"Wrapping the plugin slot {slot.Name}", true);
            InventoryItemContainer itemContainer =
                _inventoryItemContainerScene.Instantiate<InventoryItemContainer>();
            _pluginHBox.AddChild(itemContainer);
            itemContainer.SetItemDisplay(display);
            display.PivotOffset = display.Size / 2;
            display.Scale = new Vector2(0.8f, 0.8f);
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

        private void OnWeaponContainerFocusEntered() => OnContainerFocusEntered(_weaponContainer);

        private void OnContainerFocusEntered(InventoryItemContainer container)
        {
            if (container.ItemDisplay.Item != null)
            {
                _descriptionPanel.DisplayItemDescription(container.ItemDisplay.Item);
            }
            else
            {
                _descriptionPanel.DisplayString("There's nothing here. How sad...");
            }
        }

        private void OnContainerFocusExited(InventoryItemContainer container) { }

        private void OnItemContainerSelected(object source, ItemSelectedEventArgs args)
        {
            DebugLogger.LogMessage($"Item container {source} selected!");
            if (
                ServiceManager
                    .Instance.GetService<PlayerService>()
                    .GetPlayer(_playerId)
                    .CanScrapItem(args.Item)
            )
            {
                EventBus.Instance.RaiseItemScrapped(args.Item);
            }
        }

        /// <summary>
        /// Called whenever the loadout display is updated.
        /// Currently only used to update the description panel if plugins move, but theoretically could be used for other stuff.
        /// </summary>
        private void OnDisplayUpdated()
        {
            if (Active)
            {
                // Get the current focus owner and update the description
                var focused = GetViewport().GuiGetFocusOwner();
                if (focused is InventoryItemContainer container)
                {
                    _descriptionPanel.DisplayItemDescription(container.Item);
                }
            }
        }

        public override void _ExitTree()
        {
            Active = false;
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
