using System;
using System.Collections.Generic;
using Events;
using Factories;
using Godot;
using Interfaces;
using Items;
using UI.Loadout;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class LoadoutPanel : PanelContainer, IListener
    {
        private int _playerId;

        // private PlayerService _service;

        private LoadoutDisplay _loadoutDisplay;

        // private PackedScene _pluginSlotScene => GD.Load<PackedScene>("uid://c122xo53cyce1");
        // private WeaponSlot _weaponSlot;
        // private List<PluginSlot> _pluginSlots = new();
        private HBoxContainer _hBox;
        private bool _initialized;

        // public WeaponSlot WeapSlot => _weaponSlot;
        // public IReadOnlyList<PluginSlot> PluginSlots => _pluginSlots.AsReadOnly();
        public HBoxContainer HBox => _hBox;
        public bool Initialized => _initialized;

        public void Initialize(int playerId)
        {
            if (!_initialized)
            {
                // _service = ServiceManager.Instance.GetService<PlayerService>();
                // _loadoutManager = loadoutManager;
                // InitializeLoadoutPanel();

                DebugLogger.LogMessage($"Initializing...", true);
                _playerId = playerId;
                _loadoutDisplay = new();
                _loadoutDisplay.Initialize(UiLayer.GetUiLayer(_playerId).LoadoutManager);
                if (_hBox != null)
                {
                    _hBox.AddChild(_loadoutDisplay.WeapSlot);
                    foreach (PluginSlot slot in _loadoutDisplay.PluginSlots)
                    {
                        _hBox.AddChild(slot);
                    }
                }
                _initialized = true;
            }
        }

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Calling Ready...", true);
            // _weaponSlot = GetNode<WeaponSlot>("%WeaponSlot");
            _hBox = GetNode<HBoxContainer>("%LoadoutHBox");

            // _hBox.AddChild(_loadoutDisplay.WeapSlot);
            // foreach (PluginSlot slot in _loadoutDisplay.PluginSlots)
            // {
            //     _hBox.AddChild(slot);
            // }

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            // EventBus.Instance.PlayerPluginsChanged += OnPlayerPluginsChanged;
            // EventBus.Instance.PlayerWeaponChanged += OnPlayerWeaponChanged;

            // _loadoutManager.PluginEquipped += OnPluginEquipped;
            // _loadoutManager.WeaponChanged += OnPlayerWeaponChanged;
            // _loadoutManager.SlotCountUpdated += OnSlotCountUpdated;
        }

        public void DisconnectSignals()
        {
            // EventBus.Instance.PlayerPluginsChanged -= OnPlayerPluginsChanged;
            // EventBus.Instance.PlayerWeaponChanged -= OnPlayerWeaponChanged;

            // _loadoutManager.PluginEquipped -= OnPluginEquipped;
            // _loadoutManager.WeaponChanged -= OnPlayerWeaponChanged;
        }

        // private void InitializeLoadoutPanel()
        // {
        //     ClearPluginSlotItems();

        //     // Player player = _service.GetPlayer(_playerId);
        //     int slotCount = _loadoutManager.GetSlotCount();
        //     for (int i = 0; i < slotCount; i++)
        //     {
        //         AddSlot();
        //     }

        //     int pluginCount = _loadoutManager.GetPlugins().Count;
        //     if (pluginCount > 0)
        //     {
        //         DebugLogger.LogMessage(
        //             "Player initial plugins list was greater than 0! Adding plugins to UI...",
        //             true
        //         );
        //         for (int i = 0; i < pluginCount; i++)
        //         {
        //             FillSlot(_pluginSlots[i], _loadoutManager.GetPlugins()[i]);
        //         }
        //     }

        //     // Set the weapon plugin slot
        //     FillSlot(_weaponSlot, _loadoutManager.GetWeaponPlugin());

        //     _initialized = true;
        // }

        // private void FillSlot(PluginSlot slot, Plugin plugin)
        // {
        //     DebugLogger.LogMessage($"Filling slot {slot} with plugin {plugin.ResourceName}", true);
        //     slot.SetPlugin(plugin);
        // }

        // /// <summary>
        // /// Adds a plugin slot to the Loadout Panel UI.
        // /// </summary>
        // private void AddSlot()
        // {
        //     // PluginSlot pluginSlot = _pluginSlotScene.Instantiate<PluginSlot>();

        //     PluginSlot pluginSlot = ItemSlotFactory.CreatePluginSlot();

        //     // Create a unique name for the slot so that it can easily be accessed by name
        //     pluginSlot.Name = $"PluginSlot{_pluginSlots.Count}";
        //     _pluginSlots.Add(pluginSlot);
        //     _hBox.AddChild(pluginSlot);
        // }

        // private void RemoveSlot()
        // {
        //     // Find an empty slot to remove
        //     try
        //     {
        //         int emptySlot = _pluginSlots.FindIndex(slot => slot.Empty);
        //         if (emptySlot != -1)
        //         {
        //             PluginSlot slot = _pluginSlots[emptySlot];
        //             RemoveChild(slot);
        //             _pluginSlots.RemoveAt(emptySlot);
        //             slot.QueueFree();
        //         }
        //         else
        //         {
        //             throw new InvalidOperationException(
        //                 "Could not find an empty slot to remove from plugin display! Handle decreased slot count on the player level."
        //             );
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         DebugLogger.LogMessage(e.Message, true, true);
        //     }
        // }

        // private void OnPlayerPluginsChanged(object source, PlayerPluginsChangedEventArgs args)
        // {
        //     RefreshPlugins(args.Plugins);
        // }

        // private void OnPluginEquipped(object source, PlayerPluginEquippedEventArgs args)
        // {
        //     // Find the first empty slot (checking should have already happened before the player was allowed to equip something)
        //     try
        //     {
        //         PluginSlot emptySlot = _pluginSlots.Find(slot => slot.Empty);
        //         if (emptySlot == null)
        //         {
        //             throw new ArgumentNullException(
        //                 "Could not locate an empty slot! Something may be wrong with the loadout display..."
        //             );
        //         }
        //         else
        //         {
        //             FillSlot(emptySlot, args.NewPlugin);
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         DebugLogger.LogMessage(e.Message, true, true);
        //     }
        // }

        // private void RefreshPlugins(List<Plugin> plugins)
        // {
        //     ClearPluginSlotItems();
        //     if (plugins.Count > 0)
        //     {
        //         for (int i = 0; i < plugins.Count; i++)
        //         {
        //             FillSlot(_pluginSlots[i], plugins[i]);
        //         }
        //     }
        // }

        // private void OnPlayerWeaponChanged(object source, PlayerWeaponChangedEventArgs args)
        // {
        //     RefreshWeapon(args.WeaponPlugin);
        // }

        // private void OnSlotCountUpdated(int newCount)
        // {
        //     int diff = newCount - _pluginSlots.Count;
        //     if (diff > 0)
        //     {
        //         for (int i = 0; i < diff; i++)
        //         {
        //             AddSlot();
        //         }
        //     }
        //     else if (diff < 0)
        //     {
        //         RemoveSlot();
        //     }
        //     else
        //     {
        //         return;
        //     }
        // }

        // private void RefreshWeapon(WeaponPlugin weaponPlugin)
        // {
        //     FillSlot(_weaponSlot, weaponPlugin);
        // }

        // private void ClearPluginSlotItems()
        // {
        //     foreach (PluginSlot slot in _pluginSlots)
        //     {
        //         if (slot is not WeaponSlot)
        //         {
        //             slot.ClearItem();
        //         }
        //     }
        // }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
