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
    public class LoadoutManager : IListener
    {
        private int _playerId;
        private PlayerService _service;

        public event EventHandler<PlayerPluginEquippedEventArgs> PluginEquipped;
        public event EventHandler<PlayerWeaponChangedEventArgs> WeaponChanged;
        public event Action<int> SlotCountUpdated;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            _service = ServiceManager.Instance.GetService<PlayerService>();

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerPluginEquipped += OnPlayerPluginEquipped;

            Player player = _service.GetPlayer(_playerId);
            player.GetStatManager().StatUpdated += OnPlayerStatUpdated;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerPluginEquipped -= OnPlayerPluginEquipped;

            Player player = _service.GetPlayer(_playerId);
            player.GetStatManager().StatUpdated -= OnPlayerStatUpdated;
        }

        public void OnPlayerPluginEquipped(object source, PlayerPluginEquippedEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                PluginEquipped?.Invoke(this, args);
            }
        }

        public void OnPlayerStatUpdated(object source, StatUpdatedEventArgs args)
        {
            if (args.StatType == Stats.StatType.PluginSlots)
            {
                SlotCountUpdated?.Invoke((int)args.Stat.CurrentValue);
            }
        }

        public void OnPlayerWeaponChanged(object source, PlayerWeaponChangedEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                WeaponChanged?.Invoke(this, args);
            }
        }

        public int GetSlotCount()
        {
            return _service.GetPlayer(_playerId).PluginSlots;
        }

        public WeaponPlugin GetWeaponPlugin()
        {
            return _service.GetPlayer(_playerId).WeaponPlugin;
        }

        public IReadOnlyList<Plugin> GetPlugins()
        {
            return _service.GetPlayer(_playerId).GetPlugins();
        }
    }
}
