using System;
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
        }

        public override void _Process(double delta)
        {
            if (_enabled)
            {
                SetMovementDirection();
                SetFiring();
                if (_firing)
                {
                    _player.Fire();
                }
                else
                {
                    _player.StopFire();
                }
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
                _firing = true;
            }
            else
            {
                _firing = false;
            }
        }
    }
}
