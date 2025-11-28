using System;
using Godot;

namespace Events
{
    public class CollisionEventArgs : EventArgs
    {
        /// <summary>
        /// The object the <paramref name="Source"/> is colliding with.
        /// </summary>
        public GodotObject Collider { get; }

        /// <summary>
        /// The global position where the collision occurred.
        /// </summary>
        public Vector2 GlobalCollisionPoint { get; }

        /// <summary>
        /// The normal of the collision at the collision point, i.e. the angle of the collider at the collision point. Used for reflection, bouncing, etc.
        /// </summary>
        public Vector2 CollisionNormal { get; }

        public CollisionEventArgs(
            GodotObject collider,
            Vector2 globalCollisionPoint,
            Vector2 collisionNormal
        )
        {
            Collider = collider;
            GlobalCollisionPoint = globalCollisionPoint;
            CollisionNormal = collisionNormal;
        }
    }
}
