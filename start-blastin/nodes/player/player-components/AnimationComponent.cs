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
        private AnimatedSprite2D _destructionSprite;

        public override void _Ready()
        {
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _engineEffectSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%EngineEffect");
            _engineSprite = _spriteContainer.GetNode<Sprite2D>("%Engine");
            _bodySprite = _spriteContainer.GetNode<Sprite2D>("%Body");
            _destructionSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");
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
    }
}
