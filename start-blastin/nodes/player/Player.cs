using System;
using System.Collections.Generic;
using System.Linq;
using Autoloads;
using Components;
using Effects;
using Godot;
using Interfaces;
using Items;
using PlayerComponents;
using Projectiles;
using Stats;

namespace Entities
{
    [GlobalClass]
    public partial class Player : CharacterBody2D, IDie, IHealthful, IVelocityProvider, IStats
    {
        private StatManager _stats = new();
        private AnimationComponent _animationComponent;
        private MovementComponent _movementComponent;
        private WeaponComponent _weaponComponent;
        private CollisionShape2D _hitBox;
        private PlayerController _controller;
        private List<Modifier> _modifiers = new();

        // private HealthComponent _healthComponent;
        private float _maxHealth => _stats.GetStat(StatType.MaxHealth).CurrentValue;
        private float _currentHealth;
        private float _speed => _stats.GetStat(StatType.Speed).CurrentValue;

        // ~ Weapon Variables ~ //

        private ProjectileType _projType;
        private float _damage => _stats.GetStat(StatType.Damage).CurrentValue;
        private float _fireRate => _stats.GetStat(StatType.FireRate).CurrentValue;
        private float _projectileSpeed => _stats.GetStat(StatType.ProjectileSpeed).CurrentValue;

        //-----------------------------//

        [Export(PropertyHint.Range, "1,100,1,or_greater")]
        public float MaxHealth
        {
            get => _maxHealth;
            set
            {
                if (value > 0)
                {
                    _stats.UpdateStat(StatType.MaxHealth, value);
                }
                else
                {
                    _stats.UpdateStat(StatType.MaxHealth, 1);
                }
            }
        }

        public float CurrentHealth
        {
            get => _currentHealth;
            private set
            {
                if (value > _maxHealth)
                {
                    _currentHealth = _maxHealth;
                }
                else
                {
                    _currentHealth = value;
                }
            }
        }

        [Export(PropertyHint.Range, "0,2000,1,greater_than")]
        public float Speed
        {
            get => _speed;
            set
            {
                if (value <= 0)
                {
                    _stats.UpdateStat(StatType.Speed, 0);
                }
                else
                {
                    _stats.UpdateStat(StatType.Speed, value);
                }
            }
        }

        public ProjectileType ProjectileType
        {
            get => _projType;
            set => _projType = value;
        }

        /// <summary>
        /// The damage done by this weapon.
        /// </summary>
        [Export]
        public float Damage
        {
            get => _damage;
            set => _stats.UpdateStat(StatType.Damage, value);
        }

        /// <summary>
        /// Rate of fire for the weapon, used in the FireTimer.
        /// Lower values mean a faster fire rate.
        /// </summary>
        [Export]
        public float FireRate
        {
            get => _fireRate;
            set
            {
                if (value < 0.06)
                {
                    _stats.UpdateStat(StatType.FireRate, 0.05f);
                }
                else
                {
                    _stats.UpdateStat(StatType.FireRate, value);
                }
            }
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

        [Signal]
        public delegate void PlayerDiedEventHandler();

        public bool Dying = false;

        // public void Heal(float healAmount) => _healthComponent.Heal(healAmount);

        public void Fire() => _weaponComponent.FireWeapon();

        public void StopFire() => _weaponComponent.StopWeapon();

        public Vector2 GetCurrentVelocity()
        {
            return Velocity;
        }

        public StatManager GetStatManager() => _stats;

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
        }

        private void InitializeComponents()
        {
            _animationComponent.Initialize(this);
            _movementComponent.Initialize(this);
            _controller.Initialize(this);
            _weaponComponent.Initialize(this);
        }

        private void ConnectSignals()
        {
            // Connect signals
            _stats.Connect(
                StatManager.SignalName.StatUpdated,
                Callable.From(
                    (StatType statType, Stat stat) =>
                        _weaponComponent.Weapon.UpdateWeaponStats(statType, stat)
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

        public void TakeDamage(float damage)
        {
            _animationComponent.PlayDamageAnimation();
            // _healthComponent.TakeDamage(damage);
            _currentHealth -= damage;

            GD.Print($"Player has taken damage! Current health: {_currentHealth}");

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        public void Heal(float healAmount)
        {
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        }

        public void Die()
        {
            _controller.Enabled = false;
            Dying = true;
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
            }
        }

        public void AddModifier(params Modifier[] modifiers)
        {
            if (_modifiers != null)
            {
                _modifiers.AddRange(modifiers);
                ApplyModifiers();
            }
        }

        /// <summary>
        /// Applies all equipped modifiers, starting with addition operations and ending with multiplicative operations.
        /// Starts with base values of all stats and applies the changes to each stat's current values.
        /// </summary>
        private void ApplyModifiers()
        {
            // Sort the StatEffects by operation
            List<StatEffect> addStatEffects = new();
            List<StatEffect> multiplyStatEffects = new();

            // Sort the stat effect modifiers into add and multiply opertions
            foreach (Modifier modifier in _modifiers)
            {
                foreach (Effect effect in modifier.Effects)
                {
                    if (effect is StatEffect statEffect)
                    {
                        if (statEffect.Operation == Operation.Add)
                        {
                            addStatEffects.Add(statEffect);
                        }
                        else if (statEffect.Operation == Operation.Multiply)
                        {
                            multiplyStatEffects.Add(statEffect);
                        }
                    }
                }
            }

            // Create a new dictionary for the final values and initialize it with the base values for each stat.
            Dictionary<StatType, float> finalValues = new();
            foreach (KeyValuePair<StatType, Stat> kvp in _stats.Stats)
            {
                finalValues[kvp.Key] = kvp.Value.BaseValue;
            }

            // Perform add operations
            foreach (StatEffect addEffect in addStatEffects)
            {
                if (finalValues.ContainsKey(addEffect.Type))
                {
                    finalValues[addEffect.Type] += addEffect.Value;
                    GD.Print(
                        $"Adding {addEffect.Value} to {addEffect.Type}. New final value = {finalValues[addEffect.Type]}"
                    );
                }
                else
                {
                    finalValues[addEffect.Type] = addEffect.Value;
                }
            }

            // Then perform multiplication operations.
            foreach (StatEffect multiplyEffect in multiplyStatEffects)
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
    }
}
