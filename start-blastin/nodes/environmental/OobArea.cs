using System.Reflection;
using Enemies;
using Godot;
using Projectiles;

namespace Environmental
{
    /// <summary>
    /// Area2D representing an out of bounds area.
    /// Objects entering this area are despawned using the object's internal despawn logic.
    /// </summary>
    public partial class OobArea : Area2D
    {
        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            AreaEntered += OnAreaEntered;
        }

        private void OnBodyEntered(Node2D body)
        {
            if (body is EnemyNode enemy)
            {
                enemy.QueueFree();
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            if (area.GetParent() is Projectile projectile)
            {
                projectile.ToggleActive(false);
            }
        }
    }
}
