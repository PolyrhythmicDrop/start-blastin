using Components;
using Godot;
using Projectiles;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponNode : Node2D
    {
        private WeaponStats _stats;
        private ProjectilePool _pool;

        public WeaponStats Stats => _stats;
        public ProjectilePool Pool => _pool;
        public Node ProjectileParent;
        public Timer FireTimer;
        public Callable HitCallable;

        public WeaponNode()
        {
            HitCallable = Callable.From(
                (CollisionComponent collision) => OnProjectileCollision(collision)
            );
        }

        public void InitializeStats(WeaponStats stats)
        {
            _stats = stats;
        }

        public void InitializeProjectilePool()
        {
            _pool = new ProjectilePool(this, 5);
            ProjectileParent = new();
            AddChild(ProjectileParent);
        }

        public void InitializeFireTimer()
        {
            FireTimer = new();
            if (Stats == null)
            {
                GD.PrintErr($"Stats is null in {Name} before setting FireTimer.WaitTime!");
            }
            else
            {
                FireTimer.WaitTime = Stats.FireRate;
            }
            AddChild(FireTimer);
        }

        public virtual void OnProjectileCollision(CollisionComponent collision) { }
    }
}
