using Components;
using Godot;

namespace Projectiles
{
    public abstract partial class Projectile : Node2D
    {
        public bool Active { get; set; }

        public Timer DeactivationTimer { get; set; }

        public virtual float Speed { get; set; }

        [Signal]
        public delegate void CollisionEventHandler(CollisionComponent collision);

        public Projectile()
        {
            DeactivationTimer = new Timer();
            DeactivationTimer.WaitTime = 5;
        }

        public override void _Ready()
        {
            if (!IsAncestorOf(DeactivationTimer))
            {
                AddChild(DeactivationTimer);
            }
            DeactivationTimer.Start();
        }
    }
}
