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
            // GD.Print($"{body} has entered out of bounds!");
            if (body is EnemyNode enemy)
            {
                // GD.Print($"Freeing {enemy} because it has entered out of bounds!");
                enemy.QueueFree();
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            // GD.Print(
            //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: {area} entered! Deducing type..."
            // );
            if (area is Projectile projectile)
            {
                // GD.Print(
                //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: {projectile.Name} entered! Deactivating..."
                // );
                projectile.ToggleActive(false);
            }
        }
    }
}
