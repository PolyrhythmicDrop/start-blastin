using System;
using Entities;
using Godot;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class MovementComponent : Node
    {
        private Player _player;
        private float _speed = 300;

        [Export]
        public float Speed
        {
            get => _speed;
            set { _speed = value > 1 ? value : 1; }
        }

        public void Initialize(Player player)
        {
            _player = player;
        }

        public Vector2 SetVelocity(float xInput, float yInput)
        {
            return new Vector2(xInput * _speed, yInput * _speed);
        }
    }
}
