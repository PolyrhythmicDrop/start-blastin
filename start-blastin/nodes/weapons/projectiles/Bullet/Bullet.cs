using System.Reflection;
using Godot;

namespace Projectiles
{
    public partial class Bullet : Projectile
    {
        public static string ScenePath => "res://nodes/weapons/projectiles/Bullet/Bullet.tscn";
        private AnimatedSprite2D _sprite;

        public override void _Ready()
        {
            base._Ready();
            _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        }
    }
}
