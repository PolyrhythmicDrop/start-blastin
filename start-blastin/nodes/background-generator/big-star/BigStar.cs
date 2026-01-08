using Godot;
using Interfaces;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class BigStar : CelestialBody, IColorScheme
    {
        public BigStar()
        {
            _minScale = 0.8f;
            _maxScale = 2.0f;
        }

        protected new AnimatedSprite2D _sprite;

        public override void _Ready()
        {
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

        public override void _Process(double delta)
        {
            MoveLocalY(SPEED * (float)delta, false);
        }
    }
}
