using Godot;

namespace Projectiles
{
    [GlobalClass]
    public partial class Flame : DeflectableProjectile
    {
        private CollisionShape2D _collShape;
        private AnimatedSprite2D _sprite;

        public override void _Ready()
        {
            base._Ready();
            _ignoreOtherProjectiles = true;

            _collShape = GetNode<CollisionShape2D>("%CollisionShape2D");
            _sprite = GetNode<AnimatedSprite2D>("%FlameSprite");

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            if (
                !_sprite.IsConnected(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    _deactivateCallable
                )
            )
            {
                _sprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished, _deactivateCallable);
            }
        }

        public override void ToggleActive(bool active)
        {
            base.ToggleActive(active);

            if (active)
            {
                _sprite.Play();
            }
        }
    }
}
