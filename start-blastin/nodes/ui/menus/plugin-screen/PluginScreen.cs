using System;
using System.Collections.Generic;
using Godot;
using Interfaces;
using Services;
using UI.HUD;
using Utility;

namespace UI.Loadout
{
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
        private PackedScene _pricePanelScene = GD.Load<PackedScene>("uid://b2fpxs1cgq4hk");

        private Color _sellPriceColor;

        public int PlayerId => _playerId;

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
            // _weaponSlot.GrabFocus();
            // Active = true;
        }

        public void Activate(bool activate)
        {
            if (activate)
            {
                Active = true;
                Visible = true;
                _weaponSlot.GrabFocus();
            }
            else
            {
                Visible = false;
                Active = false;
            }
        }

        public void BuildPluginScreen()
        {
            AddVBoxes();
            foreach (PluginSlot slot in _pluginSlots)
            {
                slot.PivotOffset = slot.Size / 2;
                slot.Scale = new Vector2(0.8f, 0.8f);
                AddNamePanelToSlot(slot);
                AddScrapPricePanelToSlot(slot);
            }
            AddNamePanelToSlot(_weaponSlot);
            AddScrapPricePanelToSlot(_weaponSlot);
        }

        private void AddVBoxes()
        {
            var hBoxChildren = _loadoutPanel.HBox.GetChildren();
            foreach (PluginSlot slot in hBoxChildren)
            {
                VBoxContainer vBox = new VBoxContainer();
                _loadoutPanel.HBox.AddChild(vBox);
                slot.Reparent(vBox);
                vBox.Name = $"{slot.Name}VBox";
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

        private void AddScrapPricePanelToSlot(PluginSlot slot)
        {
            // VBoxContainer vBox = GetSlotVBox(slot);
            VBoxContainer vBox = GetSlotElement<VBoxContainer>(slot);
            if (vBox != null)
            {
                PricePanelContainer pricePanel =
                    _pricePanelScene.Instantiate<PricePanelContainer>();
                pricePanel.Name = GetPanelName<PricePanelContainer>(slot, true);
                vBox.AddChild(pricePanel);
                pricePanel.SetMode(PricePanelContainer.Mode.Inventory);
                pricePanel.Owner = this;
                pricePanel.UniqueNameInOwner = true;
                pricePanel.TogglePanelVisibility(false, PricePanelContainer.PriceLabel.Flux);
                if (slot.Plugin != null)
                {
                    pricePanel.SetLabelText(
                        slot.Plugin.ScrapValue.ToString(),
                        PricePanelContainer.PriceLabel.Bytes
                    );
                    pricePanel.SetFontColor(_sellPriceColor);
                }
                else
                {
                    pricePanel.TogglePanelVisibility(false);
                }
                InitializePricePanel(pricePanel);
            }
        }

        private void InitializePricePanel(PricePanelContainer panel)
        {
            // Set the name panel as invisible until it gains focus.
            Color modColor = new(panel.Modulate);
            modColor.A = 0;
            panel.Modulate = modColor;
            // Set the name panel's pivot offset to center
            panel.PivotOffset = panel.Size / 2;
            // Set the initial scane to 2
            panel.Scale = new Vector2(2, 2);
        }

        private void AddNamePanelToSlot(PluginSlot slot)
        {
            // VBoxContainer vBox = GetSlotVBox(slot);
            VBoxContainer vBox = GetSlotElement<VBoxContainer>(slot);
            if (vBox != null)
            {
                ItemNamePanelContainer namePanel =
                    _itemNameScene.Instantiate<ItemNamePanelContainer>();
                namePanel.NameLabelSettings = ItemNameLabelSettings.Inventory;
                namePanel.Name = GetPanelName<ItemNamePanelContainer>(slot, true);
                vBox.AddChild(namePanel);
                vBox.MoveChild(namePanel, 0);
                namePanel.Owner = this;
                namePanel.UniqueNameInOwner = true;
                DebugLogger.LogMessage(
                    $"{namePanel} UniqueNameInOwner set to {namePanel.UniqueNameInOwner}. Owner: {namePanel.Owner}"
                );
                if (slot.Plugin != null)
                {
                    namePanel.Label.Text = slot.Plugin.Name;
                }
                else
                {
                    namePanel.Label.Text = "Empty";
                }
                InitializeNamePanel(namePanel);
            }
        }

        private string GetPanelName<T>(PluginSlot slot, bool set)
            where T : PanelContainer
        {
            string panelType = "";
            if (typeof(T) == typeof(ItemNamePanelContainer))
            {
                panelType = "NamePanel";
            }
            else if (typeof(T) == typeof(PricePanelContainer))
            {
                panelType = "PricePanel";
            }

            if (string.IsNullOrEmpty(panelType))
            {
                DebugLogger.LogMessage(
                    $"Could not get panel name. Pass a PricePanelContainer or a ItemNamePanelContainer as this method's type parameter.",
                    true,
                    true
                );
                return null;
            }

            string name = $"{slot.Name}{panelType}";
            name = set ? name : "%" + name;
            return name;
        }

        private void InitializeNamePanel(ItemNamePanelContainer namePanel)
        {
            // Set the stylebox and label settings
            namePanel.SetStyle();
            // Set the name panel as invisible until it gains focus.
            Color modColor = new(namePanel.Modulate);
            modColor.A = 0;
            namePanel.Modulate = modColor;
            // Set the name panel's pivot offset to center
            namePanel.PivotOffset = namePanel.Size / 2;
            // Set the initial scane to 2
            namePanel.Scale = new Vector2(2, 2);
        }

        private void OnWeaponSlotFocusEntered() => OnSlotFocusEntered(_weaponSlot);

        private void OnSlotFocusEntered(PluginSlot slot)
        {
            GetSlotElements(
                slot,
                out VBoxContainer vbox,
                out ItemNamePanelContainer namePanel,
                out PricePanelContainer pricePanel
            );
            if (namePanel != null)
            {
                Tween tween = CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(namePanel, "modulate:a", 1.0, 0.3);
                tween.TweenProperty(namePanel, "scale", Vector2.One, 0.3);
                tween.TweenProperty(slot, "scale", Vector2.One, 0.3);
                tween.TweenProperty(pricePanel, "modulate:a", 1.0, 0.3);
            }

            // Set the description to the item description.
            if (slot.Plugin != null)
            {
                _descriptionPanel.DisplayItemDescription(slot.Plugin);
            }
            else
            {
                _descriptionPanel.DisplayString("An empty plugin slot. How sad...");
            }
        }

        private void OnWeaponSlotFocusExited() => OnSlotFocusExited(_weaponSlot);

        private void OnSlotFocusExited(PluginSlot slot)
        {
            GetSlotElements(
                slot,
                out VBoxContainer vbox,
                out ItemNamePanelContainer namePanel,
                out PricePanelContainer pricePanel
            );
            if (namePanel != null)
            {
                Tween tween = namePanel.CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(namePanel, "modulate:a", 0, 0.3);
                tween.TweenProperty(namePanel, "scale", new Vector2(2, 2), 0.3);
                tween.TweenProperty(slot, "scale", new Vector2(0.8f, 0.8f), 0.3);
                tween.TweenProperty(pricePanel, "modulate:a", 0, 0.3);
            }
        }

        private void GetSlotElements(
            PluginSlot slot,
            out VBoxContainer vBox,
            out ItemNamePanelContainer namePanel,
            out PricePanelContainer pricePanel
        )
        {
            vBox = GetSlotElement<VBoxContainer>(slot);
            namePanel = GetSlotElement<ItemNamePanelContainer>(slot);
            pricePanel = GetSlotElement<PricePanelContainer>(slot);
        }

        private T GetSlotElement<T>(PluginSlot slot)
            where T : Container
        {
            if (typeof(T) == typeof(VBoxContainer))
            {
                return slot.GetParentOrNull<VBoxContainer>() as T;
            }
            else if (typeof(T) == typeof(ItemNamePanelContainer))
            {
                return GetNodeOrNull<ItemNamePanelContainer>(
                        GetPanelName<ItemNamePanelContainer>(slot, false)
                    ) as T;
            }
            else if (typeof(T) == typeof(PricePanelContainer))
            {
                return GetNodeOrNull<PricePanelContainer>(
                        GetPanelName<PricePanelContainer>(slot, false)
                    ) as T;
            }
            else
            {
                DebugLogger.LogMessage(
                    $"Could not find an appropriate element type to get! Passed type: {typeof(T)}",
                    true,
                    true
                );
                return null;
            }
        }

        public override void _ExitTree()
        {
            Active = false;
            DisconnectSignals();
            _loadoutPanel.QueueFree();
            base._ExitTree();
        }
    }
}
