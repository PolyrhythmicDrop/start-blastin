using System;
using System.Collections.Generic;
using Autoloads;
using Events;
using Factories;
using Godot;
using Interfaces;
using Items;
using Services;
using Utility;

namespace UI.Loadout
{
    /// <summary>
    /// Class responsible for populating loadout displays with slotted items a player has equipped.
    /// </summary>
    public class LoadoutDisplay : IListener
    {
        private int _playerId;
        private PlayerService _service;
        private ItemDisplay _weaponSlot;
        private List<ItemDisplay> _pluginDisplays = new();

        public ItemDisplay WeapSlot => _weaponSlot;

        public IReadOnlyList<ItemDisplay> PluginDisplays => _pluginDisplays.AsReadOnly();

        private Plugin _blankPlugin = ResourceLoader.Load<Plugin>("uid://cdf365jvnlftb");

        public event Action DisplayUpdated;

        /// <summary>
        /// Finalizer to disconnect event subscriptions when the LoadoutDisplay is being garbage collected
        /// </summary>
        ~LoadoutDisplay()
        {
            DisconnectSignals();
        }

        #region Init
        public void Initialize(int playerId)
        {
            _playerId = playerId;
            _service = ServiceManager.Instance.GetService<PlayerService>();
            _weaponSlot = ItemDisplayFactory.CreateEmptyItemDisplay();
            SetWeaponSlot();

            InitializePluginSlots();

            ConnectSignals();

            DebugLogger.LogMessage($"Initialization complete!", true);
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerPluginEquipped += OnPluginEquipped;
            EventBus.Instance.PlayerWeaponChanged += OnPlayerWeaponChanged;
            EventBus.Instance.PlayerItemRemoved += OnItemRemoved;
            _service.GetPlayer(_playerId).GetStatManager().StatUpdated += OnPlayerStatUpdated;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerPluginEquipped -= OnPluginEquipped;
            EventBus.Instance.PlayerWeaponChanged -= OnPlayerWeaponChanged;
            EventBus.Instance.PlayerItemRemoved -= OnItemRemoved;
            _service.GetPlayer(_playerId).GetStatManager().StatUpdated -= OnPlayerStatUpdated;
        }

        private void InitializePluginSlots()
        {
            int slotCount = _service.GetPlayer(_playerId).PluginSlots;
            for (int i = 0; i < slotCount; i++)
            {
                AddSlot();
            }
            IReadOnlyList<Plugin> plugins = _service.GetPlayer(_playerId).GetPlugins();
            int pluginCount = plugins.Count;
            if (pluginCount > 0)
            {
                for (int i = 0; i < pluginCount; i++)
                {
                    FillSlot(_pluginDisplays[i], plugins[i]);
                }
            }
        }
        #endregion

        #region Slot Management
        private void SetWeaponSlot()
        {
            FillSlot(_weaponSlot, _service.GetPlayer(_playerId).WeaponPlugin);
        }

        private void AddSlot()
        {
            // Create a blank plugin slot display
            ItemDisplay itemDisplay = ItemDisplayFactory.CreateDisplayForItem(_blankPlugin);

            // Create a unique name for the slot so that it can easily be accessed by name
            _pluginDisplays.Add(itemDisplay);
            itemDisplay.Name = $"PluginSlot{_pluginDisplays.Count}";
        }

        private void RemoveSlot()
        {
            // Find an empty slot to remove
            try
            {
                int empty = _pluginDisplays.FindIndex(slot => slot.Item == _blankPlugin);
                if (empty != -1)
                {
                    ItemDisplay display = _pluginDisplays[empty];
                    _pluginDisplays.RemoveAt(empty);
                    // This only removes the display from the list of displays.
                    // You still need to have whatever is watching the LoadoutDisplay remove the display from its scene tree.
                    // Consider emitting an event here.
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

        private void FillSlot(ItemDisplay display, Plugin plugin)
        {
            display.SetItem(plugin);
            DisplayUpdated?.Invoke();
        }

        private void FillPluginSlotGaps()
        {
            for (int i = 0; i < _pluginDisplays.Count; i++)
            {
                if (_pluginDisplays[i].Empty)
                {
                    // Find the next plugin in the list that is not empty
                    for (int p = i + 1; p < _pluginDisplays.Count; p++)
                    {
                        // If the next plugin slot is not empty...
                        if (!_pluginDisplays[p].Empty)
                        {
                            // ...Grab the plugin and move it to to the empty slot
                            Plugin pluginToMove = _pluginDisplays[p].Item as Plugin;
                            _pluginDisplays[i].SetItem(pluginToMove);

                            // Set the slot the plugin just moved out of to blank.
                            _pluginDisplays[p].SetItem(_blankPlugin);
                            // Break out of this for loop and return to the first one to check for the next empty plugin slot.
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Event Callbacks

        private void OnPluginEquipped(object source, PlayerPluginEquippedEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                try
                {
                    ItemDisplay emptySlot = _pluginDisplays.Find(slot => slot.Empty);
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
        }

        private void OnPlayerWeaponChanged(object source, PlayerWeaponChangedEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                SetWeaponSlot();
            }
        }

        private void OnItemRemoved(object source, PlayerItemRemovedEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                if (args.Item is not WeaponPlugin && args.Item is Plugin removedPlugin)
                {
                    // Find the slot this plugin belongs to and set it to a blank plugin
                    _pluginDisplays.Find(slot => slot.Item == removedPlugin).SetItem(_blankPlugin);

                    // Shift plugins to fill gap
                    FillPluginSlotGaps();
                }
                DisplayUpdated?.Invoke();
            }
        }

        private void OnPlayerStatUpdated(object source, StatUpdatedEventArgs args)
        {
            if (args.StatType == Stats.StatType.PluginSlots)
            {
                OnSlotCountUpdated((int)args.Stat.CurrentValue);
            }
        }

        private void OnSlotCountUpdated(int newCount)
        {
            int diff = newCount - _pluginDisplays.Count;
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
        }
        #endregion
    }
}
