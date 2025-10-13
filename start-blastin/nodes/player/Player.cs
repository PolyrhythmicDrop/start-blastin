using System;
using Components;
using Godot;
using Interfaces;
using PlayerComponents;

namespace Entities
{
    [GlobalClass]
    public partial class Player : CharacterBody2D, IDie, IHealthful
    {
        private AnimationComponent _animationComponent;
        private MovementComponent _movementComponent;
        private WeaponComponent _weaponComponent;
        private CollisionShape2D _hitBox;
        private PlayerController _controller;
        private HealthComponent _healthComponent;

        [Export]
        public HealthComponent HealthComponent
        {
            get => _healthComponent;
            set => _healthComponent = value;
        }

        public bool Dying = false;

        public void TakeDamage(int damage) => _healthComponent.TakeDamage(damage);

        public void Heal(int healAmount) => _healthComponent.Heal(healAmount);

        public void Fire() => _weaponComponent.FireWeapon();

        public void StopFire() => _weaponComponent.StopWeapon();

        private void InitializeComponents()
        {
            _animationComponent.Initialize(this);
            _movementComponent.Initialize(this);
            _controller.Initialize(this);
            _weaponComponent.Initialize(this);
            _healthComponent.Initialize(this);
        }

        public override void _Ready()
        {
            _animationComponent = GetNode<AnimationComponent>("%AnimationComponent");
            _hitBox = GetNode<CollisionShape2D>("%HitBox");
            _movementComponent = GetNode<MovementComponent>("%MovementComponent");
            _controller = GetNode<PlayerController>("%PlayerController");
            _weaponComponent = GetNode<WeaponComponent>("%WeaponComponent");

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

        public void Die()
        {
            Dying = true;
            _hitBox.Disabled = true;
            _animationComponent.PlayDieAnimation();
        }

        public void Despawn()
        {
            GD.Print("Game over, man! Game over!");
            QueueFree();
        }
    }
}
