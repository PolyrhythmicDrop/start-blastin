using System;
using Autoloads;
using Entities;
using Godot;

namespace PlayerComponents
{
    public partial class PlayerController : Node
    {
        private Player _player;
        private float _xDir;
        private float _yDir;
        private bool _firing;
        private bool _enabled = true;

        public float xDirection => _xDir;
        public float yDirection => _yDir;
        public bool Firing => _firing;
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public void Initialize(Player player)
        {
            _player = player;
            ConnectSignals();
        }

        private void ConnectSignals()
        {
            EventBus.Instance.ShopOpened += () =>
            {
                _xDir = 0;
                _yDir = 0;
                _enabled = false;
            };

            EventBus.Instance.ShopClosed += () =>
            {
                _enabled = true;
            };
        }

        public override void _Process(double delta)
        {
            if (_enabled)
            {
                SetMovementDirection();
                SetPhase();
                SetFiring();
            }
        }

        public void SetMovementDirection()
        {
            _xDir = Input.GetAxis("move-left", "move-right");
            _yDir = Input.GetAxis("move-up", "move-down");
        }

        public void SetFiring()
        {
            if (Input.IsActionPressed("fire"))
            {
                _player.Fire();
            }
            else if (Input.IsActionJustReleased("fire") || !Input.IsActionPressed("fire"))
            {
                _player.StopFire();
            }
        }

        public void SetPhase()
        {
            if (Input.IsActionPressed("phase"))
            {
                _player.StartPhase();
            }
        }
    }
}
