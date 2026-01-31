using System;
using Godot;
using Interfaces;

namespace Components
{
    [GlobalClass]
    public partial class StaticDeflector : StaticBody2D, IDeflector
    {
        private Sprite2D _sprite;

        private CollisionPolygon2D _collisionPoly;

        public bool DeflectActive { get; set; } = true;

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("%DeflectorSprite");
            _collisionPoly = GetNode<CollisionPolygon2D>("%DeflectorPoly");
        }
    }
}
