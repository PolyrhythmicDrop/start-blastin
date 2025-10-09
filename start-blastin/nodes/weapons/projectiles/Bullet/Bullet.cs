using Components;
using Godot;

namespace Projectiles
{
    public partial class Bullet : Projectile
    {
        private AnimatedSprite2D _sprite;
        private RayCast2D _ray;

        public RayCast2D Ray => _ray;
        public static string ScenePath => "res://nodes/weapons/projectiles/Bullet/Bullet.tscn";

        public override void _Ready()
        {
            base._Ready();
            _sprite = GetNode<AnimatedSprite2D>("Sprite");
            _ray = GetNode<RayCast2D>("RayCast2D");
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Active)
            {
                CastRay(delta);
                Position += _speed * (float)delta * Vector2.Up;
            }
        }

        /// <summary>
        /// Casts a ray in the direction of movement to detect collisions and emit impact signals.
        /// </summary>
        /// <param name="delta">The physics frame delta time.</param>
        public void CastRay(double delta)
        {
            var nextPos = ToLocal(Speed * (float)delta * Vector2.Up);
            Ray.TargetPosition = nextPos;
            if (Ray.Enabled == false)
            {
                Ray.Enabled = true;
            }

            Ray.ForceRaycastUpdate();

            if (Ray.IsColliding())
            {
                CollisionComponent collision = new CollisionComponent()
                {
                    Source = this,
                    Collider = Ray.GetCollider(),
                    GlobalCollisionPoint = Ray.GetCollisionPoint(),
                    CollisionNormal = Ray.GetCollisionNormal() * -1,
                };

                EmitSignal(SignalName.Collision, collision);
            }
        }

#nullable enable
        /// <summary>
        /// Called when the node exits the scene tree. Disconnects signals and disables the ray.
        /// </summary>
        public override void _ExitTree()
        {
            if (Ray.Enabled == true)
            {
                Ray.Enabled = false;
            }
            base._ExitTree();
        }
#nullable disable
    }
}
