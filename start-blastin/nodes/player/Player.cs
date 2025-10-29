using System;
using System.Collections.Generic;
using System.Linq;
using Autoloads;
using Components;
using Effects;
using Enemies;
using Godot;
using Interfaces;
using Items;
using PlayerComponents;
using Projectiles;
using Services;
using Stats;
using Utility;

namespace Entities
{
    [GlobalClass]
    public partial class Player : CharacterBody2D, IDie, IHealthful, IVelocityProvider, IStats
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

        private ProjectileType _projType;
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
                _service.UpdateCurrentHealth(_playerId, _currentHealth);
            }
        }

        [Export(PropertyHint.Range, "0,2000,1,greater_than")]
        public float Speed
        {
            get => _speed;
            set => _stats.UpdateStat(StatType.Speed, Mathf.Max(0, value));
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
            set => _stats.UpdateStat(StatType.PhaseSpeed, Mathf.Max(0, value));
        }

        [ExportGroup("Weapon Stats")]
        [Export]
        public ProjectileType ProjectileType
        {
            get => _projType;
            set => _projType = value;
        }

        /// <summary>
        /// The damage done by the player's weapon.
        /// </summary>
        [Export]
        public float Damage
        {
            get => _damage;
            set => _stats.UpdateStat(StatType.Damage, Mathf.Max(0, value));
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
        /// The base speed of a projectile coming out of this weapon.
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

        [ExportGroup("Equipment Stats")]
        [Export]
        public int PluginSlots
        {
            get => _pluginSlots;
            set => _stats.UpdateStat(StatType.PluginSlots, value);
        }

        [Export]
        public Godot.Collections.Array<Plugin> Plugins
        {
            get => [.. _plugins];
            set
            {
                _plugins = [.. value];
                _service.UpdateEquippedPlugins(_playerId, _plugins);
            }
        }

        [ExportGroup("Currency")]
        [Export(PropertyHint.Range, "0,10000,10,greater_than")]
        public int Bytes
        {
            get => _bytes;
            set
            {
                _bytes = Math.Max(0, value);
                _service.UpdatePlayerCurrency(_playerId, bytes: _bytes);
            }
        }

        [Export(PropertyHint.Range, "0,10000,10,greater_than")]
        public int Flux
        {
            get => _flux;
            set
            {
                _flux = Math.Max(0, value);
                _service.UpdatePlayerCurrency(_playerId, flux: _flux);
            }
        }

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

        public override void _Ready()
        {
            _animationComponent = GetNode<AnimationComponent>("%AnimationComponent");
            _hitBox = GetNode<CollisionShape2D>("%HitBox");
            _movementComponent = GetNode<MovementComponent>("%MovementComponent");
            _controller = GetNode<PlayerController>("%PlayerController");
            _weaponComponent = GetNode<WeaponComponent>("%WeaponComponent");
            _currentHealth = _maxHealth;
            _service.UpdateCurrentHealth(_playerId, _currentHealth);

            InitializeComponents();
            ConnectSignals();

            DebugLogger.LogMessage(
                $"Phase cooldown after InitializeComponents: {_stats.GetStat(StatType.PhaseCooldown).Type} | {_stats.GetStat(StatType.PhaseCooldown).CurrentValue} | {_stats.GetStat(StatType.PhaseCooldown).BaseValue}",
                true
            );
            ApplyStatEffects();
        }

        private void InitializeComponents()
        {
            _animationComponent.Initialize(this);
            _movementComponent.Initialize(this);
            _controller.Initialize(this);
            _weaponComponent.Initialize(this);

            // Initialize plugin slots
            _plugins.Capacity = (int)_stats.GetStat(StatType.PluginSlots).CurrentValue;
            DebugLogger.LogMessage(
                $"Plugin capacity: {_plugins.Capacity} | Plugin slot count: {_pluginSlots} | Equipped plugins: {_plugins.Count}",
                true
            );
        }

        private void ConnectSignals()
        {
            // Connect signals
            _stats.Connect(
                StatManager.SignalName.StatUpdated,
                Callable.From(
                    (StatType statType, Stat stat) =>
                    {
                        if (
                            statType == StatType.FireRate
                            || statType == StatType.Damage
                            || statType == StatType.ProjectileSpeed
                        )
                        {
                            _weaponComponent.Weapon.UpdateWeaponStats(statType, stat);
                        }
                        else if (
                            statType == StatType.MaxHealth
                            || statType == StatType.PhaseCooldown
                        )
                        {
                            UpdatePlayerServiceStats(statType, stat.CurrentValue);
                        }
                    }
                )
            );

            EventBus.Instance.Connect(
                EventBus.SignalName.ShopItemBought,
                Callable.From(
                    (Item item) =>
                    {
                        BuyItem(item);
                    }
                )
            );
        }

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
                DebugLogger.LogMessage($"Starting dodge!");
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
            DebugLogger.LogMessage($"Ending phase!");
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

        public void TakeDamage(float damage)
        {
            _animationComponent.PlayDamageAnimation();
            // _healthComponent.TakeDamage(damage);
            _currentHealth -= damage;

            GD.Print($"Player has taken damage! Current health: {_currentHealth}");
            _service.UpdateCurrentHealth(_playerId, _currentHealth);

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        public void Heal(float healAmount)
        {
            DebugLogger.LogMessage(
                $"{Name} is healing. Current health: {_currentHealth} | Heal amount: {healAmount} | Max health: {_maxHealth}",
                true
            );
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
            _service.UpdateCurrentHealth(_playerId, _currentHealth);
        }

        public void Die()
        {
            _controller.Enabled = false;
            _isDying = true;
            _hitBox.Disabled = true;
            _animationComponent.PlayDieAnimation();
        }

        public void Despawn()
        {
            GD.Print("Game over, man! Game over!");
            EmitSignal(SignalName.PlayerDied);
            QueueFree();
        }

        private void BuyItem(Item item)
        {
            switch (item)
            {
                case Modifier modifier:
                    AddModifier(modifier);
                    break;
                case Plugin plugin:
                    AddPlugin(plugin);
                    break;
            }
            ApplyStatEffects();
        }

        public void AddModifier(params Modifier[] modifiers)
        {
            if (_modifiers != null)
            {
                _modifiers.AddRange(modifiers);
            }
        }

        public void AddPlugin(params Plugin[] plugins)
        {
            foreach (Plugin newPlugin in plugins)
            {
                DebugLogger.LogMessage(
                    $"Attempting to buy {newPlugin.Name}...\nCurrent plugin count: {_plugins.Count} | Total plugin slots: {_pluginSlots}",
                    true
                );

                if (_plugins.Count < _pluginSlots)
                {
                    _plugins.Add(newPlugin);
                    DebugLogger.LogMessage(
                        $"Plugin {newPlugin.Name} equipped! Current plugin count: {_plugins.Count} | Total plugin slots: {_pluginSlots}",
                        true
                    );
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
            _service.UpdateEquippedPlugins(_playerId, _plugins);
        }

        public List<Plugin> GetPlugins()
        {
            return _plugins;
        }

        public bool HasPlugin(Plugin plugin)
        {
            return _plugins.Contains(plugin);
        }

        /// <summary>
        /// Applies StatEffects from all equipped items, starting with addition operations and ending with multiplicative operations.
        /// Starts with base values of all stats and applies the changes to each stat's current values.
        /// </summary>
        private void ApplyStatEffects()
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
                SortEffects(plugin.Effects, addStatEffects, multiplyStatEffects);
            }

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
                        DebugLogger.LogMessage(
                            $"Adding {statEffect.Value} for {statEffect.Type} to addEffects List...",
                            true
                        );
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
                    GD.Print(
                        $"Multiplying {multiplyEffect.Value} on {multiplyEffect.Type}. New final value = {finalValues[multiplyEffect.Type]}"
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

        /// <summary>
        /// Updates the <see cref="PlayerService"/> with the new values of a specific stat for the player.
        /// </summary>
        /// <param name="statType">The stat type that was updated.</param>
        /// <param name="value">The new value of the stat.</param>
        private void UpdatePlayerServiceStats(StatType statType, float value)
        {
            switch (statType)
            {
                case StatType.MaxHealth:
                    _service.UpdateMaxHealth(_playerId, value);
                    break;
                case StatType.PhaseCooldown:
                    _service.UpdatePhaseCooldown(_playerId, value);
                    break;
            }
        }
    }
}
