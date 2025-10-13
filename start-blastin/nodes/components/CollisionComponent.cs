using Godot;

namespace Components
{
#nullable enable
    public partial class CollisionComponent : Node
    {
        /// <summary>
        /// The source of the collision.
        /// </summary>
        public Node2D? Source;

        /// <summary>
        /// The object the <paramref name="Source"/> is colliding with.
        /// </summary>
        public Node? Collider;

        /// <summary>
        /// The global position where the collision occurred.
        /// </summary>
        public Vector2? GlobalCollisionPoint;

        /// <summary>
        /// The normal of the collision at the collision point, i.e. the angle of the collider at the collision point. Used for reflection, bouncing, etc.
        /// </summary>
        public Vector2? CollisionNormal;
    }
#nullable disable
}
