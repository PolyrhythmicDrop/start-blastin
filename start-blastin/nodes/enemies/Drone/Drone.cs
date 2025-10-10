using System.Reflection;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class Drone : EnemyNode
    {
        private Node2D _spriteContainer;
        private AnimatedSprite2D _base;
        private AnimatedSprite2D _engine;
        private AnimatedSprite2D _destruction;

        private Vector2 _currentPosition;
        private Vector2 _lastPosition;

        public override void _Ready()
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            );
            base._Ready();
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _base = _spriteContainer.GetNode<AnimatedSprite2D>("%Base");
            _engine = _spriteContainer.GetNode<AnimatedSprite2D>("%Engine");
            _destruction = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");

            _currentPosition = _characterBody.GlobalPosition;
            _lastPosition = _currentPosition;
        }

        public override void _Process(double delta)
        {
            _lastPosition = _currentPosition;
            _currentPosition = _characterBody.GlobalPosition;

            // if (_pathFollow.ProgressRatio <= 0.2)
            // {
            //     PrintRotation();
            // }

            base._Process(delta);
            SetMoveAnimation();
        }

        private void SetMoveAnimation()
        {
            if (_currentPosition != _lastPosition)
            {
                _engine.Play("moving");
            }
            else
            {
                _engine.Play("idle");
            }
        }

        protected override void FireWeapon()
        {
            base.FireWeapon();
            _base.Animation = "fire";

            _base.AnimationFinished += () =>
            {
                _base.Animation = "default";
            };
        }
    }
}
