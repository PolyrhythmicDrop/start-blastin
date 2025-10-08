using System;
using Animation;
using Godot;

namespace Entities
{
    [GlobalClass]
    public partial class Player : CharacterBody2D
    {
        private AnimationComponent _animationComponent;
        private MovementComponent _movementComponent;
        private CollisionShape2D _hitBox;
        private PlayerController _controller;

        private void InitializeComponents()
        {
            _animationComponent.Initialize(this);
            _movementComponent.Initialize(this);
            _controller.Initialize(this);
        }

        public override void _Ready()
        {
            _animationComponent = GetNode<AnimationComponent>("%AnimationComponent");
            _hitBox = GetNode<CollisionShape2D>("%HitBox");
            _movementComponent = GetNode<MovementComponent>("%MovementComponent");
            _controller = GetNode<PlayerController>("%PlayerController");
            InitializeComponents();
        }

        public override void _Process(double delta)
        {
            Move();
        }

        public void Move()
        {
            Velocity = _movementComponent.SetVelocity(
                _controller.xDirection,
                _controller.yDirection
            );

            MoveAndSlide();
        }
    }
}
