using System;
using Enemies;
using Factories;
using Godot;
using Stats;
using UI;

namespace Components
{
    public partial class EnemyHealthComponent : Node
    {
        private EnemyNode _enemy;

        private bool _initialized;

        // overhead-health-bar.tscn
        private PackedScene _healthBarScene = ResourceLoader.Load<PackedScene>(
            "uid://dyem7stdeqti4"
        );

        private OverheadHealthBar _healthBar;

        public OverheadHealthBar HealthBar => _healthBar;

        // Current health
        private float _currentHealth;

        public float CurrentHealth
        {
            get => _currentHealth;
            set
            {
                _currentHealth = value;
                _healthBar?.SetValues(_maxHealth, _currentHealth);
            }
        }

        // Max health

        private float _baseMaxHealth;
        public float BaseMaxHealth => _baseMaxHealth;

        public float _maxHealth => _enemy.Stats.GetStat(StatType.MaxHealth).CurrentValue;

        public float MaxHealth
        {
            get => _maxHealth;
            set
            {
                _enemy?.Stats.UpdateStat(StatType.MaxHealth, Mathf.Max(1, value));
                _healthBar?.SetValues(_maxHealth, _currentHealth);
            }
        }

        /// <summary>
        /// Initializes the health node. Sets the owner to the passed enemy and sets max health based on the resource.
        /// </summary>
        /// <param name="enemy">The owner of this health component.</param>
        /// <param name="resource">The <see cref="EnemyResource"/> used to set the MaxHealth of this enemy.</param>
        public void Initialize(EnemyNode enemy, EnemyResource resource)
        {
            _enemy = enemy;

            _baseMaxHealth = resource.MaxHealth;
            _currentHealth = _baseMaxHealth;
        }

        /// <summary>
        /// Called after <see cref="Initialize"/>. Sets up the health bar.
        /// </summary>
        public override void _Ready()
        {
            // Initialize the health bar.
            _healthBar = _healthBarScene.Instantiate<OverheadHealthBar>();
            _enemy.AddChild(_healthBar);
            _healthBar.Initialize(_enemy);
            SetHealthBarSize();
        }

        #region Health Bar

        /// <summary>
        /// Turn the health bar on and off.
        /// </summary>
        public virtual void ToggleHealthBarActive()
        {
            _healthBar.ToggleActive();
        }

        /// <summary>
        /// Sets the position of the health bar based on the enemy's current position.
        /// </summary>
        public virtual void SetHealthBarPosition()
        {
            _healthBar.SetPosition(_enemy.GlobalPosition);
        }

        /// <summary>
        /// Sets the size of the enemy's health bar based on the size of the enemy's sprite. Override in derived classes.
        /// </summary>
        protected virtual void SetHealthBarSize()
        {
            AnimatedSprite2D sprite = _enemy.GetPrimarySprite();

            // Get size of the base sprite
            SpriteFrames frames = sprite.SpriteFrames ?? null;
            if (sprite != null)
            {
                Rect2I usedRect = frames.GetFrameTexture("default", 0).GetImage().GetUsedRect();
                _healthBar.SetSizeAndOffset(usedRect.Size);
            }
        }

        #endregion

        #region Damage

        /// <summary>
        /// Causes this enemy node to take damage.
        /// </summary>
        /// <param name="damage">The amount of damage to take.</param>
        /// <param name="playerId">If a player caused the damage, the <see cref="Player.PlayerId"/> of the damaging player.</param>
        public void TakeDamage(float damage, int? playerId = null)
        {
            if (_enemy.IsAlive)
            {
                // Play the hit sound
                _enemy.AudioComp.PlayHitSound();

                if (damage != 0)
                {
                    _enemy.PlayDamageAnimation();
                    IndicatorFactory.CreateTextIndicator(
                        (MathF.Round(damage, 1) * -1).ToString(),
                        _enemy.GlobalPosition,
                        parent: _enemy
                    );
                    CurrentHealth -= damage;
                }

                if (_currentHealth <= 0)
                {
                    CurrentHealth = 0;
                    _enemy.Die(playerId);
                }
            }
        }

        public void Heal(float healAmount)
        {
            // Don't do anything if current health is greater than max health
            if (_currentHealth >= _maxHealth)
            {
                return;
            }
            CurrentHealth = Mathf.Min(_currentHealth + healAmount, MaxHealth);
        }

        #endregion
    }
}
