using Godot;
using Interfaces;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class BigStar : CelestialBody, IColorScheme
    {
        public BigStar()
        {
            _minScale = 0.05f;
            _maxScale = 1.1f;

            _minSpeed = 0.5f;
            _maxSpeed = 20f;
        }

        protected new AnimatedSprite2D _sprite;

        public new AnimatedSprite2D Sprite => _sprite;

        public override void _Ready()
        {
            if (SetSpeed <= 0)
            {
                SetSpeed = (float)GD.RandRange(_minSpeed, _maxSpeed);
            }

            _sprite = GetNode<AnimatedSprite2D>("%BigStarSprite");
            _sprite.SpriteFrames = (SpriteFrames)_sprite.SpriteFrames.Duplicate(true);
            _sprite.Frame = GD.RandRange(0, 5);

            base._Ready();

            _sprite.Play();
        }

        protected override void AddVisibleNotifier()
        {
            // Get the size of the current frame.
            Texture2D texture = _sprite.SpriteFrames.GetFrameTexture("default", _sprite.Frame);
            Rect2 rect = new(_sprite.Position, texture.GetSize());

            AddChild(_visibleNotifier);
            _visibleNotifier.Rect = rect;

            _visibleNotifier.ShowRect = false;
        }

        public void ApplyColorScheme(GradientTexture1D scheme)
        {
            if (_sprite.Material is ShaderMaterial starMaterial)
            {
                starMaterial.SetShaderParameter("colorscheme", scheme);
            }
        }
    }
}
