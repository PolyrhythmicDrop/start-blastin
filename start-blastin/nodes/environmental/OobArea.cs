using System.Reflection;
using System.Threading.Tasks;
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
            BodyExited += OnBodyExited;
            AreaEntered += OnAreaEntered;
        }

        private async void OnBodyEntered(Node2D body)
        {
            if (body is EnemyNode enemy && enemy.Spawning == false)
            {
                await enemy.Weapon.WaitForAllProjectilesDisabled();
                enemy.QueueFree();
            }
        }

        private void OnBodyExited(Node2D body)
        {
            if (body is EnemyNode enemy && enemy.Spawning == true)
            {
                enemy.Spawning = false;
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            if (area is Projectile projectile)
            {
                projectile.ToggleActive(false);
            }
        }
    }
}
