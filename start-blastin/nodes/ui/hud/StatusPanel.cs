using System;
using Autoloads;
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
            // Connect the max health update signal
            Callable maxHealthChangeCallable = Callable.From(
                (int id, float max) =>
                {
                    UpdateMaxHealth(id, max);
                }
            );
            if (
                !EventBus.Instance.IsConnected(
                    EventBus.SignalName.PlayerMaxHealthChanged,
                    maxHealthChangeCallable
                )
            )
            {
                EventBus.Instance.Connect(
                    EventBus.SignalName.PlayerMaxHealthChanged,
                    maxHealthChangeCallable
                );
            }

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
        }

        private void InitializeStatusBars()
        {
            _service.GetPlayerHealth(_playerId, out float currentHealth, out float maxHealth);

            _healthBar.MaxValue = maxHealth;
            _healthBar.Value = currentHealth;
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
    }
}
