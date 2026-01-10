using System;
using Autoloads;
using Entities;
using Events;
using Godot;
using Interfaces;
using Services;
using Utility;
using WaveManagement;

namespace UI.HUD
{
    [GlobalClass]
    public partial class StatusPanel : PanelContainer, IListener
    {
        private int _playerId;
        private PlayerService _service;
        private HealthBar _healthBar;
        private PhaseBar _phaseBar;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            InitializeStatusBars();
        }

        public override void _Ready()
        {
            _service = ServiceManager.Instance.GetService<PlayerService>();
            _healthBar = GetNode<HealthBar>("%HealthBar");
            _phaseBar = GetNode<PhaseBar>("%PhaseBar");

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerMaxHealthChanged += OnPlayerMaxHealthChanged;
            EventBus.Instance.PlayerCurrentHealthChanged += OnPlayerCurrentHealthChanged;
            EventBus.Instance.PlayerPhaseCooldownChanged += OnPlayerPhaseCooldownChanged;
            EventBus.Instance.PlayerPhaseCooldownTimeLeft += OnPlayerPhaseTimeLeft;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerMaxHealthChanged -= OnPlayerMaxHealthChanged;
            EventBus.Instance.PlayerCurrentHealthChanged -= OnPlayerCurrentHealthChanged;
            EventBus.Instance.PlayerPhaseCooldownChanged -= OnPlayerPhaseCooldownChanged;
            EventBus.Instance.PlayerPhaseCooldownTimeLeft -= OnPlayerPhaseTimeLeft;
        }

        private void InitializeStatusBars()
        {
            if (_service.HasPlayer(_playerId))
            {
                Player player = _service.GetPlayer(_playerId);
                _healthBar.InitializeHealthBar(player.CurrentHealth, player.MaxHealth);
                _phaseBar.InitializePhaseBar(player.PhaseCooldown, player.PhaseCooldown);
            }
        }

        private void OnPlayerMaxHealthChanged(
            object source,
            PlayerMaxHealthChangedEventArgs args
        ) => UpdateMaxHealth(args.PlayerId, args.MaxHealth);

        private void OnPlayerCurrentHealthChanged(
            object source,
            PlayerCurrentHealthChangedEventArgs args
        ) => UpdateCurrentHealth(args.PlayerId, args.CurrentHealth, args.Difference);

        private void OnPlayerPhaseCooldownChanged(
            object source,
            PlayerPhaseCooldownChangedEventArgs args
        ) => UpdatePhaseCooldown(args.PlayerId, args.CooldownTime);

        private void OnPlayerPhaseTimeLeft(
            object source,
            PlayerPhaseCooldownTimeLeftEventArgs args
        ) => UpdatePhaseTimeLeft(args.PlayerId, args.TimeLeft, args.TotalTime);

        /// <summary>
        /// Updates the player's max health on the status bar.
        /// </summary>
        /// <param name="playerId">The ID of the player, matched against this panel's PlayerID.</param>
        /// <param name="maxHealth">The new maximum health value for the player.</param>
        private void UpdateMaxHealth(int playerId, float maxHealth)
        {
            if (playerId == _playerId)
            {
                _healthBar.SetMaxHealth(maxHealth);
            }
        }

        /// <summary>
        /// Updates the player's current health on the status bar.
        /// </summary>
        /// <param name="playerId">The ID of the player, matched against this panel's PlayerID.</param>
        /// <param name="currentHealth">The new current health value for the player.</param>
        private void UpdateCurrentHealth(int playerId, float currentHealth, float difference)
        {
            if (playerId == _playerId)
            {
                _healthBar.SetCurrentHealth(currentHealth, difference);
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
                bool phaseReady = _service.GetPlayer(_playerId).State.PhaseReady;
                _phaseBar.SetTotalCooldown(totalCooldown, phaseReady);
            }
        }

        /// <summary>
        /// Updates the player's phase bar with the remaining cooldown time.
        /// </summary>
        /// <param name="playerId">The ID of the player.</param>
        /// <param name="timeLeft">The time left on the cooldown timer.</param>
        private void UpdatePhaseTimeLeft(int playerId, double timeLeft, double totalCooldown)
        {
            if (playerId == _playerId)
            {
                _phaseBar.SetPhaseCooldownTimeLeft(timeLeft, totalCooldown);
            }
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
