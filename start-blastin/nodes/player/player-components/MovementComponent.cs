using System;
using Autoloads;
using Enemies;
using Entities;
using Events;
using Godot;
using Interfaces;
using Utility;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class MovementComponent : Node, IListener
    {
        private Player _player;
        private float _maxSpeed => _player.Speed;
        private float _currentSpeedX = 0;
        private float _currentSpeedY = 0;

        private Timer _phaseTimer = new();
        private Timer _phaseCooldownTimer = new();

        private const float ACCEL_PER_TICK = 5000;
        private const float DECEL_PER_TICK = 10000;

        public float CurrentSpeedX => _currentSpeedX;
        public float CurrentSpeedY => _currentSpeedY;

        // public bool PhaseReady;

        #region Initialization
        public override void _Ready()
        {
            // PhaseReady = true;
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

        public void ConnectSignals()
        {
            _phaseTimer.Timeout += EndPhase;
            _phaseCooldownTimer.Timeout += OnPhaseReady;
        }

        public void DisconnectSignals()
        {
            _phaseTimer.Timeout -= EndPhase;
            _phaseCooldownTimer.Timeout -= OnPhaseReady;
        }

        #endregion

        public override void _Process(double delta)
        {
            if (!_phaseCooldownTimer.IsStopped())
            {
                EventBus.Instance.RaisePlayerPhaseCooldownTimeLeft(
                    _player.PlayerId,
                    _phaseCooldownTimer.TimeLeft,
                    _player.PhaseCooldown
                );
            }
        }

        #region Movement

        public void Move(double delta, float xDir, float yDir)
        {
            _player.Velocity = SetVelocity(xDir, yDir, delta);
            _player.MoveAndSlide();
        }

        public Vector2 SetVelocity(float xInput, float yInput, double delta)
        {
            if (xInput != 0)
            {
                AccelerateX(xInput, delta);
            }
            else if (xInput == 0 && _currentSpeedX != 0)
            {
                DecelerateX(delta);
            }

            if (yInput != 0)
            {
                AccelerateY(yInput, delta);
            }
            else if (yInput == 0 && _currentSpeedY != 0)
            {
                DecelerateY(delta);
            }

            // Instant stop if you move in the other direction.
            if ((yInput < 0 && _currentSpeedY > 0) || (yInput > 0 && _currentSpeedY < 0))
            {
                _currentSpeedY = 0;
            }

            if ((xInput < 0 && _currentSpeedX > 0) || (xInput > 0 && _currentSpeedX < 0))
            {
                _currentSpeedX = 0;
            }

            return new Vector2(_currentSpeedX, _currentSpeedY);
        }

        public void AccelerateX(float xInput, double delta)
        {
            _currentSpeedX += ACCEL_PER_TICK * ((float)delta * xInput);
            _currentSpeedX = Math.Clamp(_currentSpeedX, -_maxSpeed, _maxSpeed);
        }

        public void AccelerateY(float yInput, double delta)
        {
            _currentSpeedY += ACCEL_PER_TICK * ((float)delta * yInput);
            _currentSpeedY = Math.Clamp(_currentSpeedY, -_maxSpeed, _maxSpeed);
        }

        public void DecelerateX(double delta)
        {
            float decelAmount = DECEL_PER_TICK * (float)delta;

            if (_currentSpeedX > 0)
            {
                _currentSpeedX -= decelAmount;
                if (_currentSpeedX < 0)
                {
                    _currentSpeedX = 0;
                }
            }
            else if (_currentSpeedX < 0)
            {
                _currentSpeedX += decelAmount;
                if (_currentSpeedX > 0)
                {
                    _currentSpeedX = 0;
                }
            }
        }

        public void DecelerateY(double delta)
        {
            float decelAmount = DECEL_PER_TICK * (float)delta;

            if (_currentSpeedY > 0)
            {
                _currentSpeedY -= decelAmount;
                if (_currentSpeedY < 0)
                {
                    _currentSpeedY = 0;
                }
            }
            else if (_currentSpeedY < 0)
            {
                _currentSpeedY += decelAmount;
                if (_currentSpeedY > 0)
                {
                    _currentSpeedY = 0;
                }
            }
        }
        #endregion

        #region Phasing

        public void OnPhaseReady()
        {
            // Reset the phase cooldown bar using the current PhaseCooldown stat.
            EventBus.Instance.RaisePlayerPhaseCooldownTimeLeft(
                _player.PlayerId,
                0,
                _player.PhaseCooldown
            );
            _player.Audio.PlayPhaseReadySound();
            _player.Animation.PlayPhaseReadyEffect();
            _player.State.PhaseReady = true;
        }

        /// <summary>
        /// If the phase cooldown timer is running, adjusts its time to account for the new cooldown time.
        /// </summary>
        /// <param name="newCooldown"></param>
        public void OnPlayerPhaseCooldownChanged(float newCooldown, float origCooldown)
        {
            if (_phaseCooldownTimer.IsStopped())
            {
                return;
            }

            // Adjust the time left based on the new cooldown
            double timeElapsed = origCooldown - _phaseCooldownTimer.TimeLeft;
            double newTimeRemaining = newCooldown - timeElapsed;
            if (newTimeRemaining <= 0)
            {
                _phaseCooldownTimer.Stop();
                _phaseCooldownTimer.EmitSignal(Timer.SignalName.Timeout);
            }
            else
            {
                _phaseCooldownTimer.Start(newTimeRemaining);
            }
        }

        public void StartPhase()
        {
            if (!_player.State.CanPhase())
            {
                return;
            }

            _player.State.Phasing = true;
            _player.Speed += _player.PhaseSpeed;

            // Set collision
            _player.SetCollisionMaskValue(3, false);
            _player.SetCollisionMaskValue(5, false);
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                enemy.SetCollisionMaskValue(1, false);
            }

            _player.State.PhaseReady = false;
            _phaseTimer.Start(_player.PhaseDuration);
            EventBus.Instance.RaisePhaseStarted(_player.PlayerId);

            // Initialize the cooldown phase bar with the current cooldown stat.
            EventBus.Instance.RaisePlayerPhaseCooldownTimeLeft(
                _player.PlayerId,
                _player.PhaseCooldown,
                _player.PhaseCooldown
            );

            _player.Audio.PlayPhaseStartSound();
            _player.Animation.TogglePhaseAnimation(true);
        }

        public void EndPhase()
        {
            _player.State.Phasing = false;
            _player.Speed -= _player.PhaseSpeed;

            // Set collision
            _player.SetCollisionMaskValue(3, true);
            _player.SetCollisionMaskValue(5, true);
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                enemy.SetCollisionMaskValue(1, true);
            }

            _phaseCooldownTimer.Start(_player.PhaseCooldown);
            EventBus.Instance.RaisePhaseEnded(_player.PlayerId);

            _player.Animation.TogglePhaseAnimation(false);
        }

        #endregion

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
