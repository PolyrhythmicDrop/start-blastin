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

        /// <summary>
        /// Constructor for a collision event that includes a collision normal.
        /// </summary>
        /// <param name="collider">The object the Source is colliding with.</param>
        /// <param name="globalCollisionPoint">The point in global coordinates of the collision.</param>
        /// <param name="collisionNormal">The normal vector of the collision.</param>
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

        /// <summary>
        /// Constructor for a collision event with no collision normal. The <see cref="CollisionNormal"/> for these EventArgs is set to (0, 0).
        /// </summary>
        /// <param name="collider">The object the Source is colliding with.</param>
        /// <param name="globalCollisionPoint">The point in global coordinates of the collision.</param>
        public CollisionEventArgs(GodotObject collider, Vector2 globalCollisionPoint)
        {
            Collider = collider;
            GlobalCollisionPoint = globalCollisionPoint;
            CollisionNormal = Vector2.Zero;
        }
    }
}
