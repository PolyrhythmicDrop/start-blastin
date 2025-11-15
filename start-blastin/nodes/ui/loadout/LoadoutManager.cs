using System;
using System.Collections.Generic;
using System.Threading;
using Autoloads;
using Entities;
using Events;
using Godot;
using Interfaces;
using Items;
using Services;
using UI.HUD;

namespace UI.Loadout
{
    /// <summary>
    /// Manager for displaying a player's loadout on the HUD and in various status screens.
    /// Does not actually control the loadout, only the display of the loadout.
    /// </summary>
    public partial class LoadoutManager : Node, IListener
    {
        private int _playerId;
        private PlayerService _service;
        private IReadOnlyList<Plugin> _plugins;
        private int _slotCount;

        public IReadOnlyList<Plugin> Plugins => _plugins;

        public Action LoadoutChanged;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            _service = ServiceManager.Instance.GetService<PlayerService>();
        }

        public override void _Ready()
        {
            Player player = _service.GetPlayer(_playerId);
            _plugins = player.GetPlugins();

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerPluginsChanged += OnPlayerPluginsChanged;

            Player player = _service.GetPlayer(_playerId);
            player.GetStatManager().StatUpdated += OnPlayerStatUpdated;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerPluginsChanged -= OnPlayerPluginsChanged;

            Player player = _service.GetPlayer(_playerId);
            player.GetStatManager().StatUpdated -= OnPlayerStatUpdated;
        }

        public void OnPlayerPluginsChanged(object source, PlayerPluginsChangedEventArgs args)
        {
            _plugins = args.Plugins;
            LoadoutChanged?.Invoke();
        }

        public void OnPlayerStatUpdated(object source, StatUpdatedEventArgs args)
        {
            if (args.StatType == Stats.StatType.PluginSlots)
            {
                _slotCount = (int)args.Stat.CurrentValue;
                LoadoutChanged?.Invoke();
            }
        }

        public int GetSlotCount()
        {
            return _slotCount;
        }

        public WeaponPlugin GetWeaponPlugin()
        {
            return _service.GetPlayer(_playerId).WeaponPlugin;
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
