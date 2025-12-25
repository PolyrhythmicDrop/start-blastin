using System;
using Autoloads;
using Entities;
using Godot;
using Utility;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class MovementComponent : Node
    {
        private Player _player;
        private float _speed => _player.Speed;
        private Timer _phaseTimer = new();
        private Timer _phaseCooldownTimer = new();

        public bool PhaseReady;

        public override void _Ready()
        {
            PhaseReady = true;
            _phaseTimer = GetNode<Timer>("%PhaseTimer");
            _phaseCooldownTimer = GetNode<Timer>("%PhaseCooldownTimer");
        }

        public void Initialize(Player player)
        {
            _player = player;
            _phaseTimer.WaitTime = _player.PhaseDuration;
            _phaseCooldownTimer.WaitTime = _player.PhaseCooldown;

            ConnectSignals();
        }

        private void ConnectSignals()
        {
            _phaseTimer.Timeout += _player.EndPhase;
            _phaseCooldownTimer.Timeout += _player.OnPhaseReady;
        }

        public override void _Process(double delta)
        {
            if (!_phaseCooldownTimer.IsStopped())
            {
                EventBus.Instance.RaisePlayerPhaseTimeLeft(
                    _player.PlayerId,
                    _phaseCooldownTimer.TimeLeft
                );
            }
        }

        public Vector2 SetVelocity(float xInput, float yInput)
        {
            return new Vector2(xInput * _speed, yInput * _speed);
        }

        public void StartPhase()
        {
            PhaseReady = false;
            _phaseTimer.Start(_player.PhaseDuration);
            EventBus.Instance.RaisePhaseStarted(_player.PlayerId);
            EventBus.Instance.RaisePlayerPhaseTimeLeft(_player.PlayerId, _player.PhaseCooldown);
        }

        public void EndPhase()
        {
            _phaseCooldownTimer.Start(_player.PhaseCooldown);
            EventBus.Instance.RaisePhaseEnded(_player.PlayerId);
        }
    }
}
