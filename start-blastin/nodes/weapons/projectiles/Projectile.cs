using Components;
using Godot;

namespace Projectiles
{
    public abstract partial class Projectile : Node2D
    {
        public bool Active { get; set; }

        public Timer DeactivationTimer { get; set; }

        [Signal]
        public delegate void CollisionEventHandler(CollisionComponent collision);

        public Projectile()
        {
            DeactivationTimer = new Timer();
            DeactivationTimer.WaitTime = 5;
        }
    }
}
