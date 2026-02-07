using Godot;
using Interfaces;

namespace Projectiles
{
    public partial class Bullet : DeflectableProjectile
    {
        private AnimatedSprite2D _sprite;

        public override void _Ready()
        {
            base._Ready();
            _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        }
    }
}
