using Autoloads;
using Godot;
using Utility;

namespace Projectiles
{
    [GlobalClass]
    public partial class Flame : DeflectableProjectile
    {
        private CollisionShape2D _collShape;
        private AnimatedSprite2D _sprite;
        private Tween _tween;

        private float _animDuration;

        private const float MAX_SCALE = 2.0f;
        private const float START_SCALE = 0.3f;
        private const float MAX_ROTATE_DEG = 540;

        public override void _Ready()
        {
            base._Ready();
            _ignoreOtherProjectiles = true;
            DeactivateOnCollision = false;

            _collShape = GetNode<CollisionShape2D>("%CollisionShape2D");
            _sprite = GetNode<AnimatedSprite2D>("%FlameSprite");

            _animDuration = UtilityMethods.GetAnimationDuration(_sprite) ?? 0;

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
                _sprite.Scale = Vector2.One * START_SCALE;
                _sprite.Play();
                TweenFlame();
            }
        }

        private void TweenFlame()
        {
            if (_tween != null && _tween.IsValid())
            {
                _tween.Kill();
            }

            float endRotate = (float)RNG.GetRandomDouble(0, MAX_ROTATE_DEG);

            _tween = CreateTween().SetParallel(true);

            _tween.TweenProperty(_sprite, "scale", Vector2.One * MAX_SCALE, _animDuration);
            _tween.TweenProperty(_sprite, "rotation_degrees", endRotate, _animDuration);
        }
    }
}
