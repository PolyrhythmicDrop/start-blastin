using System;
using System.Collections.Generic;
using Events;
using Factories;
using Godot;
using Interfaces;
using Items;
using UI.HUD;
using Utility;

namespace UI.Loadout
{
    /// <summary>
    /// Class responsible for populating loadout displays with slotted items a player has equipped.
    /// </summary>
    public class LoadoutDisplay : IListener
    {
        private LoadoutManager _loadoutManager;
        private WeaponSlot _weaponSlot;
        private List<PluginSlot> _pluginSlots = new();

        public WeaponSlot WeapSlot => _weaponSlot;

        public IReadOnlyList<PluginSlot> PluginSlots => _pluginSlots.AsReadOnly();

        public void Initialize(LoadoutManager loadoutManager)
        {
            DebugLogger.LogMessage(
                $"Initializing loadout display for {this} from {loadoutManager}"
            );
            _loadoutManager = loadoutManager;
            _weaponSlot = ItemSlotFactory.CreateWeaponSlot();
            SetWeaponSlot();

            InitializePluginSlots();

            ConnectSignals();

            DebugLogger.LogMessage($"Initialization complete!", true);
        }

        public void ConnectSignals()
        {
            // EventBus.Instance.PlayerPluginsChanged += OnPlayerPluginsChanged;
            // EventBus.Instance.PlayerWeaponChanged += OnPlayerWeaponChanged;

            _loadoutManager.PluginEquipped += OnPluginEquipped;
            _loadoutManager.WeaponChanged += OnPlayerWeaponChanged;
            _loadoutManager.SlotCountUpdated += OnSlotCountUpdated;
        }

        public void DisconnectSignals()
        {
            // EventBus.Instance.PlayerPluginsChanged -= OnPlayerPluginsChanged;
            // EventBus.Instance.PlayerWeaponChanged -= OnPlayerWeaponChanged;

            _loadoutManager.PluginEquipped -= OnPluginEquipped;
            _loadoutManager.WeaponChanged -= OnPlayerWeaponChanged;
        }

        private void SetWeaponSlot()
        {
            FillSlot(_weaponSlot, _loadoutManager.GetWeaponPlugin());
        }

        private void InitializePluginSlots()
        {
            DebugLogger.LogMessage($"Initializing plugin slots for {this}", true);
            int slotCount = _loadoutManager.GetSlotCount();
            for (int i = 0; i < slotCount; i++)
            {
                AddSlot();
            }

            int pluginCount = _loadoutManager.GetPlugins().Count;
            if (pluginCount > 0)
            {
                DebugLogger.LogMessage(
                    "Player initial plugins list was greater than 0! Adding plugins to UI...",
                    true
                );
                for (int i = 0; i < pluginCount; i++)
                {
                    FillSlot(_pluginSlots[i], _loadoutManager.GetPlugins()[i]);
                }
            }
        }

        private void AddSlot()
        {
            PluginSlot pluginSlot = ItemSlotFactory.CreatePluginSlot();

            // Create a unique name for the slot so that it can easily be accessed by name
            _pluginSlots.Add(pluginSlot);
            pluginSlot.Name = $"PluginSlot{_pluginSlots.Count}";
        }

        private void RemoveSlot()
        {
            // Find an empty slot to remove
            try
            {
                int empty = _pluginSlots.FindIndex(slot => slot.Empty);
                if (empty != -1)
                {
                    PluginSlot slot = _pluginSlots[empty];
                    _pluginSlots.RemoveAt(empty);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Could not find an empty slot to remove from plugin display! Handle decreased slot count on the player level."
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        private void FillSlot(PluginSlot slot, Plugin plugin)
        {
            DebugLogger.LogMessage(
                $"Filling slot {slot.Name} with plugin {plugin.ResourceName}",
                true
            );
            slot.SetItem(plugin);
        }

        private void OnPluginEquipped(object source, PlayerPluginEquippedEventArgs args)
        {
            // Find the first empty slot (checking should have already happened before the player was allowed to equip something)
            try
            {
                PluginSlot emptySlot = _pluginSlots.Find(slot => slot.Empty);
                if (emptySlot == null)
                {
                    throw new ArgumentNullException(
                        "Could not locate an empty slot! Something may be wrong with the loadout display..."
                    );
                }
                else
                {
                    FillSlot(emptySlot, args.NewPlugin);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        private void OnPlayerWeaponChanged(object source, PlayerWeaponChangedEventArgs args)
        {
            SetWeaponSlot();
        }

        private void OnSlotCountUpdated(int newCount)
        {
            int diff = newCount - _pluginSlots.Count;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    AddSlot();
                }
            }
            else if (diff < 0)
            {
                RemoveSlot();
            }
            else
            {
                return;
            }
        }
    }
}
