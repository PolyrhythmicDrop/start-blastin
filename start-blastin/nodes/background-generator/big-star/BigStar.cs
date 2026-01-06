using Godot;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class BigStar : Sprite2D
    {
        public override void _Ready()
        {
            Texture = (Texture2D)Texture.Duplicate(true);
            float xCoord = (GD.Randi() % 5) * 25.0f;
            RegionRect = new Rect2(
                x: xCoord,
                y: RegionRect.Position.Y,
                width: RegionRect.Size.X,
                height: RegionRect.Size.Y
            );
        }
    }
}
