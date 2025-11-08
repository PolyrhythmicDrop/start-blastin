using System;
using System.Collections.Generic;
using Entities;
using Godot;
using Items;
using Services;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class LoadoutPanel : PanelContainer
    {
        private int _playerId;
        private PlayerService _service;

        private PackedScene _pluginSlotScene => GD.Load<PackedScene>("uid://c122xo53cyce1");
        private WeaponSlot _weaponSlot;
        private List<PluginSlot> _pluginSlots = new();
        private HBoxContainer _hBox;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            _service = ServiceManager.Instance.GetService<PlayerService>();
            InitializeLoadoutPanel();
        }

        public override void _Ready()
        {
            _weaponSlot = GetNode<WeaponSlot>("%WeaponSlot");
            _hBox = GetNode<HBoxContainer>("%LoadoutHBox");
        }

        private void InitializeLoadoutPanel()
        {
            Player player = _service.GetPlayer(_playerId);
            int slotCount = player.PluginSlots;
            for (int i = 0; i < slotCount; i++)
            {
                PluginSlot pluginSlot = _pluginSlotScene.Instantiate<PluginSlot>();
                _pluginSlots.Add(pluginSlot);
                _hBox.AddChild(pluginSlot);
            }

            if (player.GetPlugins().Count > 0)
            {
                DebugLogger.LogMessage(
                    "Player plugins list was greater than 0! Adding plugins to UI...",
                    true
                );
                for (int i = 0; i < player.InitialPlugins.Count; i++)
                {
                    FillSlot(_pluginSlots[i], player.InitialPlugins[i]);
                }
            }

            // Set the weapon plugin slot
            _weaponSlot.SetPlugin(player.WeaponPlugin);
        }

        private void FillSlot(PluginSlot slot, Plugin plugin)
        {
            DebugLogger.LogMessage($"Filling slot {slot} with plugin {plugin.ResourceName}");
            slot.SetPlugin(plugin);
        }
    }
}
