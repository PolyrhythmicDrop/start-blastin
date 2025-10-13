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

        private Vector2 _currentGlobalPosition;
        private Vector2 _lastGlobalPosition;

        public override void _Ready()
        {
            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            // );
            base._Ready();
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _base = _spriteContainer.GetNode<AnimatedSprite2D>("%Base");
            _engine = _spriteContainer.GetNode<AnimatedSprite2D>("%Engine");
            _destruction = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");

            _currentGlobalPosition = GlobalPosition;
            _lastGlobalPosition = _currentGlobalPosition;
        }

        public override void _Process(double delta)
        {
            _lastGlobalPosition = _currentGlobalPosition;
            _currentGlobalPosition = GlobalPosition;

            base._Process(delta);
            SetMoveAnimation();
        }

        private void SetMoveAnimation()
        {
            if (_currentGlobalPosition != _lastGlobalPosition)
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
            _base.Play("fire");
        }

        public override void Die()
        {
            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name} called!"
            // );

            _weapon.FireTimer.Stop();

            _shape.Disabled = true;

            // Make the base and engine sprites invisible.
            _base.Visible = false;
            _engine.Visible = false;

            _destruction.Visible = true;
            _destruction.Play();

            if (
                !_destruction.IsConnected(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    Callable.From(base.Die)
                )
            )
            // Free the node when the animation is finished.
            {
                _destruction.Connect(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    Callable.From(base.Die)
                );
            }
        }
    }
}
