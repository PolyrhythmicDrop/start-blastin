using System.Reflection;
using Entities;
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

        public override void _Ready()
        {
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

            KinematicCollision2D collision = MoveAndCollide(_motion, true);

            if (collision != null)
            {
                OnCrash(collision);
            }
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

        public override void PlayDamageAnimation()
        {
            string mixRatioPath = "mix_ratio";
            string currentFramePath = "current_frame";

            if (_spriteContainer.Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter(mixRatioPath, 1.0);

                Tween tween = _spriteContainer.CreateTween();
                tween.TweenMethod(
                    Callable.From(
                        (int currentFrame) =>
                            shaderMaterial.SetShaderParameter(currentFramePath, currentFrame)
                    ),
                    0,
                    30,
                    0.5
                );
                tween.TweenCallback(
                    Callable.From(() => shaderMaterial.SetShaderParameter(mixRatioPath, 0))
                );
            }
        }
    }
}
