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
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            SetMoveAnimation();
        }

        private void SetMoveAnimation()
        {
            if (_characterBody.Velocity != Vector2.Zero)
            {
                _engine.Animation = "moving";
            }
            else
            {
                _engine.Animation = "idle";
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
