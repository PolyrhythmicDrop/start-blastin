using System;
using Godot;
using Interfaces;

namespace Components
{
    /// <summary>
    /// Deflector that moves with the parent object or via code. Does not move on its own.
    /// </summary>
    [GlobalClass]
    public partial class StaticDeflector : StaticBody2D, IDeflector, IVelocityProvider
    {
        private Sprite2D _sprite;

        private CollisionPolygon2D _collisionPoly;

        public bool DeflectActive { get; set; } = true;

        public Vector2 GetCurrentVelocity()
        {
            return ConstantLinearVelocity;
        }

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("%DeflectorSprite");
            _collisionPoly = GetNode<CollisionPolygon2D>("%DeflectorPoly");
        }

        /// <summary>
        /// Removes the shader material from the sprite so we can manipulate it.
        /// Used for death animations and other purposes.
        /// </summary>
        public void RemoveSpriteShaderMaterial()
        {
            if (_sprite.Material is not ShaderMaterial)
            {
                return;
            }

            _sprite.Material = null;
        }
    }
}
