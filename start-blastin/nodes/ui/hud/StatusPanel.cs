using System;
using Autoloads;
using Entities;
using Events;
using Godot;
using Interfaces;
using Services;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class StatusPanel : PanelContainer, IListener
    {
        private int _playerId;
        private PlayerService _service;
        private ProgressBar _healthBar;
        private RichTextLabel _healthLabel;
        private Color _fullHealthColor;
        private Color _midHealthColor;
        private Color _lowHealthColor;
        private ProgressBar _phaseBar;

        [ExportCategory("Health Label Colors")]
        [Export]
        public Color FullHealthColor
        {
            get => _fullHealthColor;
            set => _fullHealthColor = value;
        }

        [Export]
        public Color MidHealthColor
        {
            get => _midHealthColor;
            set => _midHealthColor = value;
        }

        [Export]
        public Color LowHealthColor
        {
            get => _lowHealthColor;
            set => _lowHealthColor = value;
        }

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            InitializeStatusBars();
        }

        public override void _Ready()
        {
            _service = ServiceManager.Instance.GetService<PlayerService>();
            _healthBar = GetNode<ProgressBar>("%HealthBar");
            _healthLabel = GetNode<RichTextLabel>("%HealthLabel");
            _phaseBar = GetNode<ProgressBar>("%PhaseBar");

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerMaxHealthChanged += OnPlayerMaxHealthChanged;
            EventBus.Instance.PlayerCurrentHealthChanged += OnPlayerCurrentHealthChanged;
            EventBus.Instance.PlayerPhaseCooldownChanged += OnPlayerPhaseCooldownChanged;
            EventBus.Instance.PlayerPhaseTimeLeft += OnPlayerPhaseTimeLeft;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerMaxHealthChanged -= OnPlayerMaxHealthChanged;
            EventBus.Instance.PlayerCurrentHealthChanged -= OnPlayerCurrentHealthChanged;
            EventBus.Instance.PlayerPhaseCooldownChanged -= OnPlayerPhaseCooldownChanged;
            EventBus.Instance.PlayerPhaseTimeLeft -= OnPlayerPhaseTimeLeft;
        }

        private void InitializeStatusBars()
        {
            if (_service.HasPlayer(_playerId))
            {
                Player player = _service.GetPlayer(_playerId);
                _healthBar.MaxValue = player.MaxHealth;
                _healthBar.Value = player.CurrentHealth;
                SetHealthLabelText();
                _phaseBar.MaxValue = player.PhaseCooldown;
                _phaseBar.Value = player.PhaseCooldown;
            }
        }

        private void OnPlayerMaxHealthChanged(
            object source,
            PlayerMaxHealthChangedEventArgs args
        ) => UpdateMaxHealth(args.PlayerId, args.MaxHealth);

        private void OnPlayerCurrentHealthChanged(
            object source,
            PlayerCurrentHealthChangedEventArgs args
        ) => UpdateCurrentHealth(args.PlayerId, args.CurrentHealth);

        private void OnPlayerPhaseCooldownChanged(
            object source,
            PlayerPhaseCooldownChangedEventArgs args
        ) => UpdatePhaseCooldown(args.PlayerId, args.CooldownTime);

        private void OnPlayerPhaseTimeLeft(object source, PlayerPhaseTimeLeftEventArgs args) =>
            UpdatePhaseTimeLeft(args.PlayerId, args.TimeLeft);

        /// <summary>
        /// Updates the player's max health on the status bar.
        /// </summary>
        /// <param name="playerId">The ID of the player, matched against this panel's PlayerID.</param>
        /// <param name="maxHealth">The new maximum health value for the player.</param>
        private void UpdateMaxHealth(int playerId, float maxHealth)
        {
            if (playerId == _playerId)
            {
                _healthBar.MaxValue = maxHealth;
                SetHealthLabelText();
            }
        }

        private void SetHealthLabelText()
        {
            // Set the values
            double maxHealth = _healthBar.MaxValue;
            double currentHealth = _healthBar.Value;

            // Set the text
            _healthLabel.Text = $"{currentHealth} / {maxHealth}";

            // Set the color according to the percentage.
            float percent = (float)(currentHealth / maxHealth);
            DebugLogger.LogMessage($"Health percent: {percent}");
            Color color = percent switch
            {
                >= 0.8f => _fullHealthColor,
                > 0.4f => _midHealthColor,
                > 0f => _lowHealthColor,
                _ => _lowHealthColor, // fallback for 0 or negative
            };
            DebugLogger.LogMessage($"Color selected: {color}");
            _healthLabel.RemoveThemeColorOverride("default_color");
            _healthLabel.AddThemeColorOverride("default_color", color);
        }

        /// <summary>
        /// Updates the player's current health on the status bar.
        /// </summary>
        /// <param name="playerId">The ID of the player, matched against this panel's PlayerID.</param>
        /// <param name="currentHealth">The new current health value for the player.</param>
        private void UpdateCurrentHealth(int playerId, float currentHealth)
        {
            if (playerId == _playerId)
            {
                _healthBar.Value = currentHealth;
                SetHealthLabelText();
            }
        }

        /// <summary>
        /// Sets the total cooldown time for the player's phase bar.
        /// </summary>
        /// <param name="_playerId">The ID of the player.</param>
        /// <param name="totalCooldown">The new phase total cooldown value.</param>
        private void UpdatePhaseCooldown(int playerId, float totalCooldown)
        {
            if (playerId == _playerId)
            {
                _phaseBar.MaxValue = totalCooldown;
            }
        }

        /// <summary>
        /// Updates the player's phase bar with the remaining cooldown time.
        /// </summary>
        /// <param name="playerId">The ID of the player.</param>
        /// <param name="timeLeft">The time left on the cooldown timer.</param>
        private void UpdatePhaseTimeLeft(int playerId, double timeLeft)
        {
            if (playerId == _playerId)
            {
                _phaseBar.Value = _phaseBar.MaxValue - timeLeft;
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
