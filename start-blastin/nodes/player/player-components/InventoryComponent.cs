using System.Collections.Generic;
using Autoloads;
using Effects;
using Entities;
using Godot;
using Interfaces;
using Items;
using Utility;

namespace PlayerComponents
{
    /// <summary>
    /// Component that controls the player's current equipment.
    /// Includes the player's current items and methods for equipping, buying, and scrapping said items.
    /// </summary>
    [GlobalClass]
    public partial class InventoryComponent : Node, IPlayerComponent
    {
        private Player _player;

        private Shield _shield;

        private List<Modifier> _modifiers = new();
        private List<Plugin> _plugins = new();

        public IReadOnlyList<Plugin> EquippedPlugins => _plugins;

        /// <summary>
        /// The player's initial set of equipped plugins. Used for debugging.
        /// </summary>
        [Export]
        public Godot.Collections.Array<Plugin> InitialPlugins { get; set; } = new();

        private WeaponPlugin _weaponPlugin;
        private WeaponPlugin _defaultWeaponPlugin = ResourceLoader.Load<WeaponPlugin>(
            "uid://dmulsmpa1tm6h"
        );

        [ExportGroup("Weapon Stats")]
        [Export]
        public WeaponPlugin WeaponPlugin
        {
            get => _weaponPlugin;
            set => _weaponPlugin = value;
        }

        // Constructor sets the initial _weaponPlugin to the default.
        // Changes to WeaponPlugin in the editor will automatically override this.
        public InventoryComponent()
        {
            _weaponPlugin = _defaultWeaponPlugin;
        }

        #region Initialization

        /// <summary>
        /// Called before <see cref="Initialize"/>.
        /// </summary>
        public override void _Ready()
        {
            _shield = GetNode<Shield>("%Shield");
        }

        /// <summary>
        /// Called after <see cref="_Ready"/>.
        /// </summary>
        /// <param name="player"></param>
        public void Initialize(Player player)
        {
            _player = player;
            _plugins.Capacity = _player.PluginSlots;

            // Equip any initial plugins.
            EquipPlugin([.. InitialPlugins]);
        }

        #endregion

        #region Equipment Status

        public bool HasPlugin(Plugin plugin)
        {
            bool hasWeapon = _weaponPlugin?.Equals(plugin) ?? false;
            bool hasPlugin = _plugins.Find(eqPlugin => eqPlugin.Equals(plugin)) != null;
            return hasWeapon || hasPlugin;
        }

        #endregion

        #region Equipping

        public void EquipModifier(params Modifier[] modifiers)
        {
            if (_modifiers != null)
            {
                _modifiers.AddRange(modifiers);
                ApplyEquipStatEffects();
            }
        }

        public void EquipPlugin(params Plugin[] plugins)
        {
            foreach (Plugin plugin in plugins)
            {
                // Add the plugin to the plugins list and raise the PlayerPluginEquipped event for this particular plugin
                if (_plugins.Count <= _player.PluginSlots && plugin is not Items.WeaponPlugin)
                {
                    _plugins.Add(plugin);
                    EventBus.Instance.RaisePlayerPluginEquipped(_player.PlayerId, plugin);
                }
                // Swap out any weapon plugins
                else if (plugin is WeaponPlugin weaponPlugin)
                {
                    SwapWeaponPlugin(weaponPlugin);
                    EventBus.Instance.RaisePlayerWeaponChanged(_player.PlayerId, _weaponPlugin);
                }
                else
                {
                    DebugLogger.LogMessage(
                        $"Cannot add {plugin.ResourceName} to plugin list! Equipped plugin count cannot exceed plugin slots. Slots: {_player.PluginSlots} | Current equipped plugins: {_plugins.Count}",
                        true,
                        true
                    );
                }
            }

            EnablePluginEffects(plugins);

            // Apply equip effects
            ApplyEquipStatEffects();
        }

        public void SwapWeaponPlugin(WeaponPlugin weaponPlugin)
        {
            _weaponPlugin = weaponPlugin;
            _player.WeaponComp.SetWeaponProjectile(weaponPlugin.ProjectileType);

            // Set the weapon's fire sound as the player's current firing sound
            if (_weaponPlugin?.FireSound != null)
            {
                _player.Audio.Sounds.Fire = _weaponPlugin.FireSound;
            }

            EventBus.Instance.RaisePlayerWeaponChanged(_player.PlayerId, _weaponPlugin);
        }

        #endregion

        #region Unequipping

        public void ScrapItem(Item item)
        {
            // Add to the player's byte count.
            // TODO: consider adding an item that lets you scrap stuff for flux, or both currencies.
            _player.Bytes += item.ScrapValue;

            // Remove the item from the player's equipment.
            if (item is Plugin plugin)
            {
                UnequipPlugin(plugin);
            }

            // Apply stat effects based on the new loadout.
            ApplyEquipStatEffects();
        }

        public void UnequipPlugin(Plugin plugin)
        {
            DebugLogger.LogMessage($"Unequipping {plugin.ResourceName}!", true);

            DisablePluginEffects(plugin);

            if (plugin is WeaponPlugin)
            {
                // Revert to the basic bullet if you sell a weapon plugin.
                ResetWeaponPlugin();
            }
            else if (_plugins.Contains(plugin))
            {
                _plugins.Remove(plugin);
                EventBus.Instance.RaisePlayerItemRemoved(_player.PlayerId, plugin);
            }
        }

        public void DisablePluginEffects(Plugin plugin)
        {
            DebugLogger.LogMessage($"Removing effects of {plugin.ResourceName}!", true);

            foreach (Effect effect in plugin.GetEffectList())
            {
                if (effect is ChainEffect chainEffect)
                {
                    foreach (Effect nestedEffect in chainEffect.GetAllEffects())
                    {
                        nestedEffect.Disable(this);
                    }
                }
                effect.Disable(this);
            }
        }

        /// <summary>
        /// Resets the player's projectile type to the base projectile.
        /// </summary>
        private void ResetWeaponPlugin()
        {
            // _weaponPlugin = ResourceLoader.Load<WeaponPlugin>("uid://dmulsmpa1tm6h");
            _weaponPlugin = _defaultWeaponPlugin;
            _player.WeaponComp.SetWeaponProjectile(_weaponPlugin.ProjectileType);
        }

        #endregion

        #region Effects

        public void EnablePluginEffects(params Plugin[] plugins)
        {
            foreach (Plugin plugin in plugins)
            {
                // Only enable the top-level effects in the plugin.
                // Nested effects are enabled by the parent ChainEffect.
                foreach (Effect effect in plugin.GetEffectList())
                {
                    effect.Enable(this);
                }
            }
        }

        /// <summary>
        /// Applies StatEffects from all equipped items, starting with addition operations and ending with multiplicative operations.
        /// Starts with base values of all stats and applies the changes to each stat's current values.
        /// </summary>
        private void ApplyEquipStatEffects()
        {
            // Sort the StatEffects by operation
            List<StatEffect> addStatEffects = new();
            List<StatEffect> multiplyStatEffects = new();

            foreach (Modifier modifier in _modifiers)
            {
                SortEffectsByOperation(modifier.Effects, addStatEffects, multiplyStatEffects);
            }

            foreach (Plugin plugin in _plugins)
            {
                List<Effect> equipEffects = plugin
                    .GetEffectList()
                    .FindAll(effect => effect.Trigger == Trigger.Equip);
                SortEffectsByOperation(equipEffects, addStatEffects, multiplyStatEffects);
            }

            // Add the weapon plugin to the mix
            List<Effect> weapEffects = _weaponPlugin
                .GetEffectList()
                .FindAll(effect => effect.Trigger == Trigger.Equip);
            SortEffectsByOperation(weapEffects, addStatEffects, multiplyStatEffects);

            _player.UpdateStatsWithEffects(addStatEffects, multiplyStatEffects);
        }

        /// <summary>
        /// Sorts <see cref="StatEffect"/>s by their operation.
        /// </summary>
        /// <param name="effects"></param>
        /// <param name="addEffects"></param>
        /// <param name="multiplyEffects"></param>
        private void SortEffectsByOperation(
            IEnumerable<Effect> effects,
            List<StatEffect> addEffects,
            List<StatEffect> multiplyEffects
        )
        {
            foreach (Effect effect in effects)
            {
                if (effect is StatEffect statEffect)
                {
                    if (statEffect.Operation == Operation.Add)
                    {
                        addEffects.Add(statEffect);
                    }
                    else if (statEffect.Operation == Operation.Multiply)
                    {
                        multiplyEffects.Add(statEffect);
                    }
                }
            }
        }

        #endregion
    }
}
