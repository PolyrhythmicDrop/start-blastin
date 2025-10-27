using System;
using Entities;
using Godot;

namespace PlayerComponents
{
    public partial class AnimationComponent : Node2D
    {
        private Player _player;
        private Node2D _spriteContainer;
        private AnimatedSprite2D _engineEffectSprite;
        private Sprite2D _engineSprite;
        private Sprite2D _bodySprite;
        private AnimatedSprite2D _dodgeSprite;
        private AnimatedSprite2D _destructionSprite;
        private ShaderMaterial _hitEffectShaderMat;

        public override void _Ready()
        {
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _engineEffectSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%EngineEffect");
            _engineSprite = _spriteContainer.GetNode<Sprite2D>("%Engine");
            _bodySprite = _spriteContainer.GetNode<Sprite2D>("%Body");
            _destructionSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");
            _dodgeSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%Dodge");
            _hitEffectShaderMat = ResourceLoader.Load<ShaderMaterial>(
                "res://resources/materials/hit-effect.tres"
            );
        }

        public void Initialize(Player player)
        {
            _player = player;
        }

        public override void _Process(double delta)
        {
            if (!_player.Dying)
            {
                if (_player.Velocity != Vector2.Zero)
                {
                    _engineEffectSprite.Play("full-power");
                }
                else
                {
                    _engineEffectSprite.Play("idle");
                }
            }
        }

        public void PlayDieAnimation()
        {
            _engineEffectSprite.Hide();
            _engineSprite.Hide();
            _bodySprite.Hide();

            _destructionSprite.Visible = true;
            _destructionSprite.Play("full-explosion");
            _destructionSprite.AnimationFinished += _player.Despawn;
        }

        public void PlayDamageAnimation()
        {
            string mixRatioPath = "mix_ratio";
            string currentFramePath = "current_frame";

            if (Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter(mixRatioPath, 1.0);

                Tween tween = CreateTween();
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

        /// <summary>
        /// Toggles the player's dodge animation on or off.
        /// </summary>
        /// <param name="on">When true, turns on the dodge animation. When false, turns it off.</param>
        public void ToggleDodgeAnimation(bool on)
        {
            if (on)
            {
                _dodgeSprite.Visible = true;
                _dodgeSprite.Play("default");
            }
            else
            {
                _dodgeSprite.Visible = false;
                _dodgeSprite.Stop();
            }
        }
    }
}
