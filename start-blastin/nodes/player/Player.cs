using System;
using System.Collections.Generic;
using Autoloads;
using Effects;
using Events;
using Factories;
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
            IWeaponOwner,
            IDeflector
    {
        private int _playerId = 1;

        #region Components
        private PlayerService _service = ServiceManager.Instance.GetService<PlayerService>();
        private StatManager _stats = new();
        private InventoryComponent _inventory;
        private AudioComponent _audioComponent;
        private AnimationComponent _animationComponent;
        private MovementComponent _movementComponent;
        private WeaponComponent _weaponComponent;
        private PlayerStateComponent _stateComponent;
        private PlayerController _controller;

        private CollisionShape2D _hitBox;

        public InventoryComponent Inventory => _inventory;
        public MovementComponent Movement => _movementComponent;
        public WeaponComponent WeaponComp => _weaponComponent;
        public WeaponNode Weapon => _weaponComponent.Weapon;
        public AnimationComponent Animation => _animationComponent;
        public AudioComponent Audio => _audioComponent;

        #endregion

        #region State
        public PlayerStateComponent State => _stateComponent;

        public bool DeflectActive
        {
            get => _stateComponent.DeflectActive;
            set => _stateComponent.DeflectActive = value;
        }

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
                if (_currentHealth != value)
                {
                    float diff = value - _currentHealth;
                    _currentHealth = MathF.Round(value, 2);
                    EventBus.Instance.RaisePlayerCurrentHealthChanged(
                        _playerId,
                        _currentHealth,
                        diff
                    );
                }
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
        /// <summary>
        /// The damage done by the player's weapon.
        /// </summary>
        /// <remarks>
        /// This value is augmented or decreased by the player's equipped WeaponPlugin.
        /// </remarks>
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
        /// <remarks>
        /// This value is augmented or decreased by the player's equipped WeaponPlugin.
        /// </remarks>
        [Export(PropertyHint.Range, "0.05,5,0.01,greater_than")]
        public float FireRate
        {
            get => _fireRate;
            set => _stats.UpdateStat(StatType.FireRate, Mathf.Max(0.05f, value));
        }

        /// <summary>
        /// The base speed of any projectile coming out of the player's weapon.
        /// Projectile speed is augmented by the firing object's speed.
        /// </summary>
        /// <remarks>
        /// This value is augmented or decreased by the player's equipped WeaponPlugin.
        /// </remarks>
        [Export(PropertyHint.Range, "0,10000,greater_than")]
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

        #endregion

        #region Initialization

        public Vector2 GetCurrentVelocity()
        {
            return Velocity;
        }

        public void SetPlayerId(int id)
        {
            _playerId = id;
        }

        public override void _Ready()
        {
            CurrentHealth = _maxHealth;

            // Set component node variables
            _audioComponent = GetNode<AudioComponent>("%AudioComponent");
            _animationComponent = GetNode<AnimationComponent>("%AnimationComponent");
            _movementComponent = GetNode<MovementComponent>("%MovementComponent");
            _controller = GetNode<PlayerController>("%PlayerController");
            _stateComponent = GetNode<PlayerStateComponent>("%PlayerStateComponent");
            _inventory = GetNode<InventoryComponent>("%InventoryComponent");
            _weaponComponent = GetNode<WeaponComponent>("%WeaponComponent");

            _hitBox = GetNode<CollisionShape2D>("%HitBox");
            if (_hitBox.Shape is ConvexPolygonShape2D convex)
            {
                convex.SetPointCloud(convex.Points);
            }

            ConnectSignals();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _audioComponent.Initialize(this);
            _animationComponent.Initialize(this);
            _movementComponent.Initialize(this);
            _controller.Initialize(this);
            _stateComponent.Initialize(this);
            _inventory.Initialize(this);
            _weaponComponent.Initialize(this);

            _inventory.EquipInitialPlugins();
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

        #endregion


        #region Weapons

        public void Fire() => _weaponComponent.FireWeapon();

        public void StopFire() => _weaponComponent.StopWeapon();

        #endregion

        #region Movement
        public override void _Process(double delta)
        {
            Move(delta);
            // if (_shield.Enabled)
            // {
            //     SetShieldVelocity();
            // }
        }

        public void Move(double delta) =>
            _movementComponent.Move(delta, _controller.xDirection, _controller.yDirection);

        public void StartPhase() => _movementComponent.StartPhase();

        public void EndPhase() => _movementComponent.EndPhase();

        #endregion

        #region Health

        public void TakeDamage(float damage, int? playerId = null)
        {
            _animationComponent.PlayDamageAnimation();
            CurrentHealth -= damage;

            IndicatorFactory.CreateTextIndicator(
                (MathF.Round(damage, 1) * -1).ToString(),
                new Vector2(GlobalPosition.X + 15, GlobalPosition.Y),
                parent: this
            );

            EventBus.Instance.RaisePlayerTakeDamage(PlayerId, damage, this);

            if (_currentHealth <= 0)
            {
                CurrentHealth = 0;
                Die();
            }
        }

        public void Heal(float healAmount)
        {
            // Don't do anything if current health is greater than max health
            if (_currentHealth >= _maxHealth)
            {
                return;
            }
            IndicatorFactory.CreateTextIndicator(
                MathF.Round(healAmount, 1).ToString(),
                new Vector2(GlobalPosition.X + 15, GlobalPosition.Y),
                parent: this
            );
            CurrentHealth = MathF.Min(_currentHealth + healAmount, _maxHealth);
        }

        public void Die(int? playerId = null)
        {
            _controller.Enabled = false;
            _stateComponent.Dying = true;
            _hitBox.Disabled = true;
            _animationComponent.PlayDieAnimation();
        }

        public void Despawn()
        {
            QueueFree();
        }

        #endregion
        #region Shop

        /// <summary>
        /// Checks to see if the player can purchase the passed item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the player is able to buy and equip the item, false if not.</returns>
        public bool CanBuyItem(Item item) => _stateComponent.CanBuyItem(item);

        /// <summary>
        /// Checks to see if the player can scrap the passed item.
        /// Currently only checks the item's <see cref="Item.Scrappable"/> variable, but putting it here to dovetail with CanBuyItem() and to make sure I can add additional checks later if necessary.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the player can scrap the item, false if not.</returns>
        public bool CanScrapItem(Item item) => _stateComponent.CanScrapItem(item);

        /// <summary>
        /// Checks to see if the player can purchase the passed item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="flux">Output that indicates whether or not the player has enough flux.</param>
        /// <param name="bytes">Output that indicates whether or not the player has enough bytes.</param>
        /// <returns>True if the player is able to buy and equip the item, false if not.</returns>
        public bool CanAffordItem(Item item, out bool flux, out bool bytes) =>
            _stateComponent.CanAffordItem(item, out flux, out bytes);

        /// <summary>
        /// Checks if the player can afford an item based on its flux and byte cost.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the player can afford the item, false if not.</returns>
        public bool CanAffordItem(Item item) => _stateComponent.CanAffordItem(item);

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
                    EquipModifier(modifier);
                    break;
                case Plugin plugin:
                    EquipPlugin(plugin);
                    break;
            }
        }

        #endregion

        #region Inventory


        private void ScrapItem(Item item) => _inventory.ScrapItem(item);

        public void UnequipPlugin(Plugin plugin) => _inventory.UnequipPlugin(plugin);

        private void DisablePluginEffects(Plugin plugin) => _inventory.DisablePluginEffects(plugin);

        public void EquipModifier(params Modifier[] modifiers) =>
            _inventory.EquipModifier(modifiers);

        public void EquipPlugin(params Plugin[] plugins) => _inventory.EquipPlugin(plugins);

        public void SwapWeaponPlugin(WeaponPlugin weaponPlugin) =>
            _inventory.EquipWeaponPlugin(weaponPlugin);

        public IReadOnlyList<Plugin> GetPlugins() => _inventory.EquippedPlugins;

        public bool HasPlugin(Plugin plugin) => _inventory.HasPlugin(plugin);

        #endregion

        #region Stats and Effects

        public StatManager GetStatManager() => _stats;

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

        public void UpdateStatsWithEffects(
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

        #region Event Handlers

        /// <summary>
        /// Callback for special processing when a stat is updated.
        /// </summary>
        /// <param name="source">The entity's StatManager instance.</param>
        /// <param name="args">Arguments from the event.</param>
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
                    DebugLogger.LogMessage($"Player phase cooldown changed to {PhaseCooldown}");
                    _movementComponent.OnPlayerPhaseCooldownChanged(
                        args.Stat.CurrentValue,
                        args.OriginalValue
                    );
                    EventBus.Instance.RaisePlayerPhaseCooldownChanged(
                        _playerId,
                        args.Stat.CurrentValue,
                        args.OriginalValue
                    );
                    break;
            }
        }

        private void OnEnemyKilled(object sender, EnemyKilledEventArgs args)
        {
            if (args.PlayerId == _playerId)
            {
                Flux += args.FluxReward;
                Bytes += args.BytesReward;
            }
        }

        private void OnItemBought(object sender, ItemBoughtEventArgs args)
        {
            BuyItem(args.Item);
        }

        private void OnItemScrapped(object sender, ItemScrappedEventArgs args)
        {
            ScrapItem(args.Item);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }

        #endregion

        // #region Shield

        // public void Block()
        // {
        //     if (!_shield.Enabled)
        //     {
        //         _shield.Enable();
        //     }
        // }

        // public void EndBlock()
        // {
        //     _shield.Disable();
        // }

        // private void SetShieldVelocity()
        // {
        //     _shield.ConstantLinearVelocity = Velocity;
        // }

        // #endregion
    }
}
