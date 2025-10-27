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
        private Timer _dodgeTimer = new();
        private Timer _dodgeCooldownTimer = new();

        public bool DodgeReady;

        public override void _Ready()
        {
            DodgeReady = true;
            _dodgeTimer = GetNode<Timer>("%DodgeTimer");
            _dodgeCooldownTimer = GetNode<Timer>("%DodgeCooldownTimer");
        }

        public void Initialize(Player player)
        {
            _player = player;
            _dodgeTimer.WaitTime = _player.DodgeDuration;
            _dodgeCooldownTimer.WaitTime = _player.DodgeCooldown;

            ConnectSignals();
        }

        private void ConnectSignals()
        {
            _dodgeTimer.Timeout += _player.EndDodge;
            _dodgeCooldownTimer.Timeout += _player.OnDodgeReady;
        }

        public Vector2 SetVelocity(float xInput, float yInput)
        {
            return new Vector2(xInput * _speed, yInput * _speed);
        }

        public void StartDodge()
        {
            DebugLogger.LogMessage(
                $"Dodge started! Dodge duration: {_player.DodgeDuration} | Dodge speed: {_player.DodgeSpeed}"
            );
            DodgeReady = false;
            _dodgeTimer.Start(_player.DodgeDuration);
        }

        public void EndDodge()
        {
            DebugLogger.LogMessage($"Dodge ending! Dodge cooldown: {_player.DodgeCooldown}");
            _dodgeCooldownTimer.Start(_player.DodgeCooldown);
        }
    }
}
