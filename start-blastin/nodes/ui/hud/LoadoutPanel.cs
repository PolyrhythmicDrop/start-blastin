using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autoloads;
using Entities;
using Events;
using Godot;
using Interfaces;
using Items;
using Services;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class LoadoutPanel : PanelContainer, IListener
    {
        private int _playerId;
        private PlayerService _service;

        private PackedScene _pluginSlotScene => GD.Load<PackedScene>("uid://c122xo53cyce1");
        private WeaponSlot _weaponSlot;
        private List<PluginSlot> _pluginSlots = new();
        private HBoxContainer _hBox;
        private bool _initialized;

        public WeaponSlot WeapSlot => _weaponSlot;
        public IReadOnlyList<PluginSlot> PluginSlots => _pluginSlots.AsReadOnly();
        public HBoxContainer HBox => _hBox;
        public bool Initialized => _initialized;

        public void Initialize(int playerId)
        {
            if (!_initialized)
            {
                _playerId = playerId;
                _service = ServiceManager.Instance.GetService<PlayerService>();
                InitializeLoadoutPanel();
            }
        }

        public override void _Ready()
        {
            _weaponSlot = GetNode<WeaponSlot>("%WeaponSlot");
            _hBox = GetNode<HBoxContainer>("%LoadoutHBox");

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerPluginsChanged += OnPlayerPluginsChanged;
            EventBus.Instance.PlayerWeaponChanged += OnPlayerWeaponChanged;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerPluginsChanged -= OnPlayerPluginsChanged;
            EventBus.Instance.PlayerWeaponChanged -= OnPlayerWeaponChanged;
        }

        private void InitializeLoadoutPanel()
        {
            ClearPluginSlotItems();

            Player player = _service.GetPlayer(_playerId);
            int slotCount = player.PluginSlots;
            for (int i = 0; i < slotCount; i++)
            {
                AddSlot();
            }

            IReadOnlyList<Plugin> playerPlugins = player.GetPlugins();
            if (playerPlugins.Count > 0)
            {
                DebugLogger.LogMessage(
                    "Player initial plugins list was greater than 0! Adding plugins to UI...",
                    true
                );
                for (int i = 0; i < playerPlugins.Count; i++)
                {
                    FillSlot(_pluginSlots[i], playerPlugins[i]);
                }
            }

            // Set the weapon plugin slot
            FillSlot(_weaponSlot, player.WeaponPlugin);

            _initialized = true;
        }

        private void FillSlot(PluginSlot slot, Plugin plugin)
        {
            DebugLogger.LogMessage($"Filling slot {slot} with plugin {plugin.ResourceName}");
            slot.SetPlugin(plugin);
        }

        /// <summary>
        /// Adds a plugin slot to the Loadout Panel UI.
        /// </summary>
        private void AddSlot()
        {
            PluginSlot pluginSlot = _pluginSlotScene.Instantiate<PluginSlot>();
            _pluginSlots.Add(pluginSlot);
            _hBox.AddChild(pluginSlot);
        }

        private void OnPlayerPluginsChanged(object source, PlayerPluginsChangedEventArgs args)
        {
            RefreshPlugins(args.Plugins);
        }

        private void RefreshPlugins(List<Plugin> plugins)
        {
            ClearPluginSlotItems();
            if (plugins.Count > 0)
            {
                DebugLogger.LogMessage("Adding plugins to UI...", true);
                for (int i = 0; i < plugins.Count; i++)
                {
                    FillSlot(_pluginSlots[i], plugins[i]);
                }
            }
        }

        private void OnPlayerWeaponChanged(object source, PlayerWeaponChangedEventArgs args)
        {
            RefreshWeapon(args.WeaponPlugin);
        }

        private void RefreshWeapon(WeaponPlugin weaponPlugin)
        {
            FillSlot(_weaponSlot, weaponPlugin);
        }

        private void ClearPluginSlotItems()
        {
            foreach (PluginSlot slot in _pluginSlots)
            {
                if (slot is not WeaponSlot)
                {
                    slot.ClearPlugin();
                }
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
