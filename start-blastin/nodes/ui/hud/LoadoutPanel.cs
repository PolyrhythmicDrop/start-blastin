using System;
using System.Collections.Generic;
using Entities;
using Godot;
using Items;
using Services;

namespace UI.HUD
{
    [GlobalClass]
    public partial class LoadoutPanel : PanelContainer
    {
        private int _playerId;
        private PlayerService _service;

        private PackedScene _pluginSlotScene => GD.Load<PackedScene>("uid://c122xo53cyce1");
        private WeaponSlot _weaponSlot;
        private List<PluginSlot> _pluginSlots;
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
                _hBox.AddChild(pluginSlot);
            }

            // Set the weapon plugin slot
            _weaponSlot.SetPlugin(player.WeaponPlugin);
        }
    }
}
