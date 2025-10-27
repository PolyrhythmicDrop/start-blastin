using System;
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

        public Vector2 SetVelocity(float xInput, float yInput)
        {
            return new Vector2(xInput * _speed, yInput * _speed);
        }

        public void StartPhase()
        {
            DebugLogger.LogMessage(
                $"Dodge started! Dodge duration: {_player.PhaseDuration} | Dodge speed: {_player.PhaseSpeed}"
            );
            PhaseReady = false;
            _phaseTimer.Start(_player.PhaseDuration);
        }

        public void EndPhase()
        {
            DebugLogger.LogMessage($"Dodge ending! Dodge cooldown: {_player.PhaseCooldown}");
            _phaseCooldownTimer.Start(_player.PhaseCooldown);
        }
    }
}
