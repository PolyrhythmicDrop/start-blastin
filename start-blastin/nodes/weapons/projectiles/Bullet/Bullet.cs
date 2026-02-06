using Godot;

namespace Projectiles
{
    public partial class Bullet : Projectile
    {
        private AnimatedSprite2D _sprite;

        public override void _Ready()
        {
            base._Ready();
            _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        }
    }
}
