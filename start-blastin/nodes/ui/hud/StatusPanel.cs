using System;
using Autoloads;
using Entities;
using Events;
using Godot;
using Services;

namespace UI.HUD
{
    [GlobalClass]
    public partial class StatusPanel : PanelContainer
    {
        private int _playerId;
        private PlayerService _service;
        private ProgressBar _healthBar;
        private ProgressBar _phaseBar;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            InitializeStatusBars();
        }

        public override void _Ready()
        {
            _healthBar = GetNode<ProgressBar>("%HealthBar");
            _phaseBar = GetNode<ProgressBar>("%PhaseBar");
            _service = ServiceManager.Instance.GetService<PlayerService>();

            ConnectSignals();
        }

        private void ConnectSignals()
        {
            EventBus.Instance.PlayerMaxHealthChanged += OnPlayerMaxHealthChanged;

            // Connect the current health update signal
            Callable currentHealthChangeCallable = Callable.From(
                (int id, float max) =>
                {
                    UpdateCurrentHealth(id, max);
                }
            );
            if (
                !EventBus.Instance.IsConnected(
                    EventBus.SignalName.PlayerCurrentHealthChanged,
                    currentHealthChangeCallable
                )
            )
            {
                EventBus.Instance.Connect(
                    EventBus.SignalName.PlayerCurrentHealthChanged,
                    currentHealthChangeCallable
                );
            }

            // Connect the phase total cooldown signal
            Callable phaseCooldownChangeCallable = Callable.From(
                (int id, float totalCooldown) =>
                {
                    UpdatePhaseCooldown(id, totalCooldown);
                }
            );
            if (
                !EventBus.Instance.IsConnected(
                    EventBus.SignalName.PlayerPhaseTotalCooldownChanged,
                    phaseCooldownChangeCallable
                )
            )
            {
                EventBus.Instance.Connect(
                    EventBus.SignalName.PlayerPhaseTotalCooldownChanged,
                    phaseCooldownChangeCallable
                );
            }

            // Connect the phase cooldown time left signal
            Callable phaseTimeLeftCallable = Callable.From(
                (int id, float timeLeft) =>
                {
                    UpdatePhaseTimeLeft(id, timeLeft);
                }
            );
            if (
                !EventBus.Instance.IsConnected(
                    EventBus.SignalName.PlayerPhaseTimeLeft,
                    phaseTimeLeftCallable
                )
            )
            {
                EventBus.Instance.Connect(
                    EventBus.SignalName.PlayerPhaseTimeLeft,
                    phaseTimeLeftCallable
                );
            }
        }

        private void DisconnectSignals()
        {
            EventBus.Instance.PlayerMaxHealthChanged -= OnPlayerMaxHealthChanged;
        }

        private void InitializeStatusBars()
        {
            if (_service.GetPlayerHealth(_playerId, out float currentHealth, out float maxHealth))
            {
                _healthBar.MaxValue = maxHealth;
                _healthBar.Value = currentHealth;
            }
            if (_service.GetPlayerPhaseCooldown(_playerId, out float totalCooldown))
            {
                _phaseBar.MaxValue = totalCooldown;
                _phaseBar.Value = totalCooldown;
            }
        }

        private void OnPlayerMaxHealthChanged(object source, PlayerMaxHealthChangedEventArgs args)
        {
            UpdateMaxHealth(args.PlayerId, args.MaxHealth);
        }

        private void UpdateMaxHealth(int playerId, float maxHealth)
        {
            if (playerId == _playerId)
            {
                _healthBar.MaxValue = maxHealth;
            }
        }

        private void UpdateCurrentHealth(int playerId, float currentHealth)
        {
            if (playerId == _playerId)
            {
                _healthBar.Value = currentHealth;
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
        private void UpdatePhaseTimeLeft(int playerId, float timeLeft)
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
