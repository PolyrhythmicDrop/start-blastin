using Godot;
using Interfaces;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class BigStar : CelestialBody, IColorScheme
    {
        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("%BigStarSprite");

            _sprite.Texture = (Texture2D)_sprite.Texture.Duplicate(true);
            float xCoord = (GD.Randi() % 5) * 25.0f;
            _sprite.RegionRect = new Rect2(
                x: xCoord,
                y: _sprite.RegionRect.Position.Y,
                width: _sprite.RegionRect.Size.X,
                height: _sprite.RegionRect.Size.Y
            );
        }

        public void ApplyColorScheme(GradientTexture1D scheme)
        {
            if (Material is ShaderMaterial starMaterial)
            {
                starMaterial.SetShaderParameter("colorscheme", scheme);
            }
        }

        public override void _Process(double delta)
        {
            MoveLocalY(SPEED * (float)delta, true);
        }
    }
}
