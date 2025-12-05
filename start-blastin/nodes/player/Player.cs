using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autoloads;
using Components;
using Effects;
using Enemies;
using Events;
using Godot;
using Interfaces;
using Items;
using PlayerComponents;
using Services;
using Stats;
using Utility;
using Weapons;

namespace Entities
{
    [GlobalClass]
    public partial class Player
        : CharacterBody2D,
            IDie,
            IHealthful,
            IVelocityProvider,
            IStats,
            IWeaponOwner
    {
        private int _playerId = 1;

        #region Components
        private PlayerService _service = ServiceManager.Instance.GetService<PlayerService>();
        private StatManager _stats = new();
        private AnimationComponent _animationComponent;
        private MovementComponent _movementComponent;
        private WeaponComponent _weaponComponent;
        private CollisionShape2D _hitBox;
        private PlayerController _controller;
        private List<Modifier> _modifiers = new();
        private List<Plugin> _plugins = new();
        private WeaponPlugin _weaponPlugin;

        private WeaponPlugin _defaultWeaponPlugin =>
            ResourceLoader.Load<WeaponPlugin>("uid://dmulsmpa1tm6h");

        #endregion
        #region Stats

        private float _maxHealth => _stats.GetStat(StatType.MaxHealth).CurrentValue;
        private float _currentHealth;
        private float _speed => _stats.GetStat(StatType.Speed).CurrentValue;
        private float _phaseDuration => _stats.GetStat(StatType.PhaseDuration).CurrentValue;
        private float _phaseCooldown => _stats.GetStat(StatType.PhaseCooldown).CurrentValue;
        private float _phaseSpeed => _stats.GetStat(StatType.PhaseSpeed).CurrentValue;

        private int _pluginSlots => (int)_stats.GetStat(StatType.PluginSlots).CurrentValue;

        // ~ Weapon Variables ~ //

        private float _damage => _stats.GetStat(StatType.Damage).CurrentValue;

        private float _crashDamage => _stats.GetStat(StatType.CrashDamage).CurrentValue;
        private float _fireRate => _stats.GetStat(StatType.FireRate).CurrentValue;
        private float _projectileSpeed => _stats.GetStat(StatType.ProjectileSpeed).CurrentValue;

        //-----------------------------//

        // ~ Currency ~ //
        private int _bytes = 0;
        private int _flux = 0;

        #endregion

        #region State

        private bool _isPhasing = false;
        private bool _phaseReady => _movementComponent.PhaseReady;
        private bool _isDying = false;

        #endregion

        public int PlayerId => _playerId;

        [Export(PropertyHint.Range, "1,100,1,or_greater")]
        public float MaxHealth
        {
            get => _maxHealth;
            set => _stats.UpdateStat(StatType.MaxHealth, Mathf.Max(1, value));
        }

        public float CurrentHealth
        {
            get => _currentHealth;
            private set
            {
                _currentHealth = Mathf.Min(value, _maxHealth);
                // _service.UpdateCurrentHealth(_playerId, _currentHealth);
                EventBus.Instance.RaisePlayerCurrentHealthChanged(_playerId, _currentHealth);
            }
        }

        [Export(PropertyHint.Range, "0,2000,1,greater_than")]
        public float Speed
        {
            get => _speed;
            set => _stats.UpdateStat(StatType.Speed, Mathf.Max(0.0f, value));
        }

        [Export]
        public float CrashDamage
        {
            get => _crashDamage;
            set => _stats.UpdateStat(StatType.CrashDamage, value);
        }

        [ExportGroup("Phase Stats")]
        [Export(PropertyHint.Range, "0.1,3,0.1,greater_than")]
        public float PhaseDuration
        {
            get => _phaseDuration;
            set => _stats.UpdateStat(StatType.PhaseDuration, Mathf.Max(0.1f, value));
        }

        [Export(PropertyHint.Range, "0.1,5,0.1,greater_than")]
        public float PhaseCooldown
        {
            get => _phaseCooldown;
            set => _stats.UpdateStat(StatType.PhaseCooldown, Mathf.Max(0.05f, value));
        }

        [Export(PropertyHint.Range, "0,2000,10,greater_than")]
        public float PhaseSpeed
        {
            get => _phaseSpeed;
            set => _stats.UpdateStat(StatType.PhaseSpeed, Mathf.Max(0.0f, value));
        }

        [ExportGroup("Weapon Stats")]
        [Export]
        public WeaponPlugin WeaponPlugin
        {
            get => _weaponPlugin;
            set => _weaponPlugin = value;
        }

        /// <summary>
        /// The damage done by the player's weapon.
        /// </summary>
        [Export]
        public float Damage
        {
            get { return _stats.GetStat(StatType.Damage).CurrentValue; }
            set
            {
                if (_stats.HasStat(StatType.Damage))
                {
                    _stats.UpdateStat(StatType.Damage, Mathf.Max(0.0f, value));
                }
                else
                {
                    _stats.AddStat(new Stat(StatType.Damage, Mathf.Max(0.0f, value)));
                }
            }
        }

        /// <summary>
        /// Rate of fire for the weapon, used in the FireTimer.
        /// Lower values mean a faster fire rate.
        /// </summary>
        [Export]
        public float FireRate
        {
            get => _fireRate;
            set => _stats.UpdateStat(StatType.FireRate, Mathf.Max(0.05f, value));
        }

        /// <summary>
        /// The base speed of any projectile coming out of the player's weapon.
        /// The player's WeaponPlugin can modify this value.
        /// </summary>
        /// <remarks>
        /// Projectile speed is augmented by the firing object's speed.
        /// </remarks>
        [Export]
        public float ProjectileSpeed
        {
            get => _projectileSpeed;
            set => _stats.UpdateStat(StatType.ProjectileSpeed, value);
        }

        /// <summary>
        /// The total number of plugin slots the player has.
        /// </summary>
        [ExportGroup("Equipment Stats")]
        [Export]
        public int PluginSlots
        {
            get => _pluginSlots;
            set => _stats.UpdateStat(StatType.PluginSlots, value);
        }

        /// <summary>
        /// The player's initial set of equipped plugins. Used for debugging.
        /// </summary>
        [Export]
        public Godot.Collections.Array<Plugin> InitialPlugins
        {
            get => [.. _plugins];
            set
            {
                foreach (Plugin plugin in value)
                {
                    EquipPlugin(plugin);
                }
            }
        }

        [ExportGroup("Currency")]
        [Export(PropertyHint.Range, "0,10000,10,greater_than")]
        public int Bytes
        {
            get => _bytes;
            set
            {
                int oldBytes = _bytes;
                _bytes = Math.Max(0, value);
                EventBus.Instance.RaisePlayerCurrencyChanged(
                    _playerId,
                    _bytes,
                    _flux,
                    bytesChange: _bytes - oldBytes
                );
            }
        }

        [Export(PropertyHint.Range, "0,10000,10,greater_than")]
        public int Flux
        {
            get => _flux;
            set
            {
                int oldFlux = _flux;
                _flux = Math.Max(0, value);
                EventBus.Instance.RaisePlayerCurrencyChanged(
                    _playerId,
                    _bytes,
                    _flux,
                    fluxChange: _flux - oldFlux
                );
            }
        }

        public WeaponNode Weapon => _weaponComponent.Weapon;

        [Signal]
        public delegate void PlayerDiedEventHandler();

        public bool Dying => _isDying;
        public bool Dodging => _isPhasing;

        public void Fire() => _weaponComponent.FireWeapon();

        public void StopFire() => _weaponComponent.StopWeapon();

        public Vector2 GetCurrentVelocity()
        {
            return Velocity;
        }

        public StatManager GetStatManager() => _stats;

        public void SetPlayerId(int id)
        {
            _playerId = id;
        }

        #region Initialization

        public override void _Ready()
        {
            _animationComponent = GetNode<AnimationComponent>("%AnimationComponent");
            _hitBox = GetNode<CollisionShape2D>("%HitBox");
            _movementComponent = GetNode<MovementComponent>("%MovementComponent");
            _controller = GetNode<PlayerController>("%PlayerController");
            _weaponComponent = GetNode<WeaponComponent>("%WeaponComponent");
            _currentHealth = _maxHealth;

            InitializeComponents();
            ConnectSignals();
            ApplyEquipStatEffects();
        }

        private void InitializeComponents()
        {
            _animationComponent.Initialize(this);
            _movementComponent.Initialize(this);
            _controller.Initialize(this);
            _weaponComponent.Initialize(this);

            if (_weaponPlugin != _defaultWeaponPlugin)
            {
                _weaponComponent.SetWeaponProjectile(_weaponPlugin.ProjectileType);
            }
            _plugins.Capacity = (int)_stats.GetStat(StatType.PluginSlots).CurrentValue;
        }

        private void ConnectSignals()
        {
            _stats.StatUpdated += OnStatUpdated;
            EventBus.Instance.ItemBought += OnItemBought;
            EventBus.Instance.ItemScrapped += OnItemScrapped;
            EventBus.Instance.EnemyKilled += OnEnemyKilled;
        }

        private void DisconnectSignals()
        {
            _stats.StatUpdated -= OnStatUpdated;
            EventBus.Instance.ItemBought -= OnItemBought;
            EventBus.Instance.ItemScrapped -= OnItemScrapped;
            EventBus.Instance.EnemyKilled -= OnEnemyKilled;
        }

        public void OnStatUpdated(object source, StatUpdatedEventArgs args)
        {
            switch (args.StatType)
            {
                case StatType.FireRate:
                case StatType.Damage:
                case StatType.ProjectileSpeed:
                    _weaponComponent.Weapon.UpdateWeaponStats(args.StatType, args.Stat);
                    break;
                case StatType.MaxHealth:
                    EventBus.Instance.RaisePlayerMaxHealthChanged(
                        _playerId,
                        args.Stat.CurrentValue
                    );
                    break;
                case StatType.PhaseCooldown:
                    EventBus.Instance.RaisePlayerPhaseCooldownChanged(
                        _playerId,
                        args.Stat.CurrentValue
                    );
                    break;
            }
        }

        #endregion

        #region Movement
        public override void _Process(double delta)
        {
            Move();
        }

        public void Move()
        {
            Velocity = _movementComponent.SetVelocity(
                _controller.xDirection,
                _controller.yDirection
            );

            MoveAndSlide();
        }

        public void StartPhase()
        {
            if (CanPhase())
            {
                _isPhasing = true;
                Speed += PhaseSpeed;

                // Set collision
                SetCollisionMaskValue(3, false);
                SetCollisionMaskValue(5, false);
                Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
                foreach (EnemyNode enemy in enemies)
                {
                    enemy.SetCollisionMaskValue(1, false);
                }

                _movementComponent.StartPhase();
                _animationComponent.TogglePhaseAnimation(true);
            }
        }

        public void EndPhase()
        {
            _isPhasing = false;
            Speed -= PhaseSpeed;

            // Set collision
            SetCollisionMaskValue(3, true);
            SetCollisionMaskValue(5, true);
            Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
            foreach (EnemyNode enemy in enemies)
            {
                enemy.SetCollisionMaskValue(1, true);
            }

            _movementComponent.EndPhase();
            _animationComponent.TogglePhaseAnimation(false);
        }

        private bool CanPhase()
        {
            return !_isPhasing && !_isDying && _phaseReady;
        }

        public void OnPhaseReady()
        {
            _animationComponent.PlayPhaseReadyEffect();
            _movementComponent.PhaseReady = true;
        }
        #endregion

        #region Health

        public void TakeDamage(float damage, int? playerId = null)
        {
            _animationComponent.PlayDamageAnimation();
            _currentHealth -= damage;

            EventBus.Instance.RaisePlayerCurrentHealthChanged(_playerId, _currentHealth);

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        public void Heal(float healAmount)
        {
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
            EventBus.Instance.RaisePlayerCurrentHealthChanged(_playerId, _currentHealth);
        }

        public void Die(int? playerId = null)
        {
            _controller.Enabled = false;
            _isDying = true;
            _hitBox.Disabled = true;
            _animationComponent.PlayDieAnimation();
        }

        public void Despawn()
        {
            EmitSignal(SignalName.PlayerDied);
            QueueFree();
        }

        #endregion
        #region Shop

        /// <summary>
        /// Checks to see if the player can purchase the passed item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the player is able to buy and equip the item, false if not.</returns>
        public bool CanBuyItem(Item item)
        {
            bool canAfford = CanAffordItem(item);
            bool noDupePlugins = _plugins.Contains(item) ? false : true;
            bool noDupeWeapon = _weaponPlugin != item;
            bool freeSlot = (_plugins.Count + 1) <= _pluginSlots;

            return canAfford && noDupePlugins && noDupeWeapon && freeSlot;
        }

        /// <summary>
        /// Checks to see if the player can scrap the passed item.
        /// Currently only checks the item's <see cref="Item.Scrappable"/> variable, but putting it here to dovetail with CanBuyItem() and to make sure I can add additional checks later if necessary.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the player can scrap the item, false if not.</returns>
        public bool CanScrapItem(Item item)
        {
            return item is not Modifier && item.Scrappable;
        }

        /// <summary>
        /// Checks to see if the player can purchase the passed item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="flux">Output that indicates whether or not the player has enough flux.</param>
        /// <param name="bytes">Output that indicates whether or not the player has enough bytes.</param>
        /// <returns>True if the player is able to buy and equip the item, false if not.</returns>
        public bool CanAffordItem(Item item, out bool flux, out bool bytes)
        {
            flux = item.FluxCost <= Flux;
            bytes = item.ByteCost <= Bytes;
            return flux && bytes;
        }

        /// <summary>
        /// Checks if the player can afford an item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the player can afford the item, false if not.</returns>
        public bool CanAffordItem(Item item)
        {
            return item.FluxCost <= _flux && item.ByteCost <= _bytes;
        }

        private void OnItemBought(object sender, ItemBoughtEventArgs args)
        {
            BuyItem(args.Item);
        }

        /// <summary>
        /// Buys and equips an item from the store.
        /// </summary>
        /// <param name="item">The item that was bought.</param>
        private void BuyItem(Item item)
        {
            // Subtract appropriate currency (currency changed event should fire automatically)
            Flux -= item.FluxCost;
            Bytes -= item.ByteCost;

            switch (item)
            {
                case Modifier modifier:
                    AddModifier(modifier);
                    break;
                case Plugin plugin:
                    EquipPlugin(plugin);
                    break;
            }
        }

        #endregion

        #region Inventory

        private void OnItemScrapped(object sender, ItemScrappedEventArgs args)
        {
            ScrapItem(args.Item);
        }

        private void ScrapItem(Item item)
        {
            // Add to the player's byte count.
            // TODO: consider adding an item that lets you scrap stuff for flux, or both currencies.
            Bytes += item.ScrapValue;

            // Remove the item from the player's equipment.

            if (item is WeaponPlugin)
            {
                // Revert to the basic bullet if you sell a weapon plugin.
                ResetWeaponPlugin();
            }
            else if (item is Plugin plugin && _plugins.Contains(plugin))
            {
                _plugins.Remove(plugin);
                EventBus.Instance.RaisePlayerItemRemoved(_playerId, plugin);
            }

            // Apply stat effects based on the new loadout.
            ApplyEquipStatEffects();
        }

        public void AddModifier(params Modifier[] modifiers)
        {
            if (_modifiers != null)
            {
                _modifiers.AddRange(modifiers);
            }
        }

        public void EquipPlugin(params Plugin[] plugins)
        {
            foreach (Plugin newPlugin in plugins)
            {
                if (_plugins.Count <= _pluginSlots && newPlugin is not Items.WeaponPlugin)
                {
                    _plugins.Add(newPlugin);
                    EventBus.Instance.RaisePlayerPluginEquipped(_playerId, newPlugin);
                }
                else if (newPlugin is WeaponPlugin weaponPlugin)
                {
                    SwapWeaponPlugin(weaponPlugin);
                }
                else
                {
                    DebugLogger.LogMessage(
                        $"Cannot add {newPlugin.Name} to plugin list! Equipped plugin count cannot exceed plugin slots. Slots: {_pluginSlots} | Current equipped plugins: {_plugins.Count}",
                        true,
                        true
                    );
                }
            }

            // Set all targets for "Self" effects to this player
            SetSelfEffectTargets(plugins);

            // Apply equip effects
            ApplyEquipStatEffects();
        }

        private void SetSelfEffectTargets(params Plugin[] plugins)
        {
            List<Effect> selfEffects = new();

            // Get all the effects that target Self and add them to the selfEffects list.
            for (int i = 0; i < plugins.Count(); i++)
            {
                selfEffects.Concat(
                    plugins[i].GetEffectList().FindAll(effect => effect.Target == TargetType.Self)
                );
            }

            // Set the target for each "self effect" to this Player.
            foreach (Effect effect in selfEffects)
            {
                effect.SetTarget(this);
            }
        }

        public void SwapWeaponPlugin(WeaponPlugin weaponPlugin)
        {
            _weaponPlugin = weaponPlugin;
            _weaponComponent.SetWeaponProjectile(weaponPlugin.ProjectileType);
            EventBus.Instance.RaisePlayerWeaponChanged(_playerId, _weaponPlugin);
        }

        /// <summary>
        /// Resets the player's projectile type to the base projectile.
        /// </summary>
        private void ResetWeaponPlugin()
        {
            _weaponPlugin = ResourceLoader.Load<WeaponPlugin>("uid://dmulsmpa1tm6h");
            _weaponComponent.SetWeaponProjectile(_weaponPlugin.ProjectileType);
            EventBus.Instance.RaisePlayerWeaponChanged(_playerId, _weaponPlugin);
        }

        public IReadOnlyList<Plugin> GetPlugins()
        {
            return _plugins.AsReadOnly();
        }

        public bool HasPlugin(Plugin plugin)
        {
            bool hasWeapon = _weaponPlugin == plugin ? true : false;
            bool hasPlugin = _plugins.Contains(plugin);
            return hasWeapon || hasPlugin;
        }

        #endregion

        #region Equipment Effects

        /// <summary>
        /// Sets a stat value based on a passed StatType.
        /// Used for Effects and other objects so you can use the correct getters/setters instead of accessing the StatManager directly.
        /// </summary>
        /// <param name="type">The stat type to set.</param>
        /// <param name="value">The new value for the stat type.</param>
        public void SetStat(StatType type, float value)
        {
            try
            {
                switch (type)
                {
                    case StatType.CrashDamage:
                        CrashDamage = value;
                        break;
                    case StatType.Damage:
                        Damage = value;
                        break;
                    case StatType.FireRate:
                        FireRate = value;
                        break;
                    case StatType.MaxHealth:
                        MaxHealth = value;
                        break;
                    case StatType.PhaseCooldown:
                        PhaseCooldown = value;
                        break;
                    case StatType.PhaseDuration:
                        PhaseDuration = value;
                        break;
                    case StatType.PhaseSpeed:
                        PhaseSpeed = value;
                        break;
                    case StatType.PluginSlots:
                        PluginSlots = (int)value;
                        break;
                    case StatType.ProjectileSpeed:
                        ProjectileSpeed = value;
                        break;
                    case StatType.Speed:
                        Speed = value;
                        break;
                    default:
                        throw new ArgumentException(
                            $"The passed StatType {type} does not have a corresponding variable!"
                        );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
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
                SortEffects(modifier.Effects, addStatEffects, multiplyStatEffects);
            }

            foreach (Plugin plugin in _plugins)
            {
                List<Effect> equipEffects = plugin
                    .GetEffectList()
                    .FindAll(effect => effect.Trigger == Trigger.Equip);
                SortEffects(equipEffects, addStatEffects, multiplyStatEffects);
            }

            // Add the weapon plugin to the mix
            List<Effect> weapEffects = _weaponPlugin
                .GetEffectList()
                .FindAll(effect => effect.Trigger == Trigger.Equip);
            SortEffects(weapEffects, addStatEffects, multiplyStatEffects);

            UpdateStatsWithEffects(addStatEffects, multiplyStatEffects);
        }

        private void SortEffects(
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

        private void UpdateStatsWithEffects(
            List<StatEffect> addEffects,
            List<StatEffect> multiplyEffects
        )
        {
            // Create a new dictionary for the final values and initialize it with the base values for each stat.
            Dictionary<StatType, float> finalValues = new();
            foreach (KeyValuePair<StatType, Stat> kvp in _stats.Stats)
            {
                finalValues[kvp.Key] = kvp.Value.BaseValue;
            }

            // Perform add operations
            foreach (StatEffect addEffect in addEffects)
            {
                if (finalValues.ContainsKey(addEffect.Type))
                {
                    finalValues[addEffect.Type] += addEffect.Value;
                    DebugLogger.LogMessage(
                        $"Adding {addEffect.Value} to {addEffect.Type}. New final value = {finalValues[addEffect.Type]}",
                        true
                    );
                }
                else
                {
                    finalValues[addEffect.Type] = addEffect.Value;
                }
            }

            // Then perform multiplication operations.
            foreach (StatEffect multiplyEffect in multiplyEffects)
            {
                if (finalValues.ContainsKey(multiplyEffect.Type))
                {
                    finalValues[multiplyEffect.Type] *= multiplyEffect.Value;
                    DebugLogger.LogMessage(
                        $"Multiplying {multiplyEffect.Value} on {multiplyEffect.Type}. New final value = {finalValues[multiplyEffect.Type]}",
                        true
                    );
                }
                else
                {
                    finalValues[multiplyEffect.Type] = 1 * multiplyEffect.Value;
                }
            }

            // Apply the new values
            foreach (KeyValuePair<StatType, float> kvp in finalValues)
            {
                _stats.UpdateStat(kvp.Key, kvp.Value);
            }
        }

        #endregion

        private void OnEnemyKilled(object sender, EnemyKilledEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                Flux += args.FluxReward;
                Bytes += args.BytesReward;
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
