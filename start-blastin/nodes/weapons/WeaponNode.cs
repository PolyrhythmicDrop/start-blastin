using System;
using Components;
using Godot;
using Projectiles;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponNode : Node2D
    {
        private WeaponStats _stats;
        private bool _enemyOwned;
        private bool _ownerSet;
        private ProjectilePool _pool;

        public WeaponStats Stats => _stats;
        public bool EnemyOwned
        {
            get => _enemyOwned;
            set
            {
                if (_ownerSet)
                {
                    throw new InvalidOperationException(
                        $"{Name}: Cannot assign an owner after owner already set!"
                    );
                }
                else
                {
                    _ownerSet = true;
                    _enemyOwned = value;
                }
            }
        }
        public ProjectilePool Pool => _pool;
        public Node ProjectileParent;
        public virtual Vector2 ProjSpawnPoint
        {
            get => GlobalPosition;
        }
        public Timer FireTimer;
        public Callable HitCallable;

        public WeaponNode()
        {
            HitCallable = Callable.From(
                (CollisionComponent collision) => OnProjectileCollision(collision)
            );
        }

        public override void _Ready()
        {
            InitializeProjectilePool();
            InitializeFireTimer();
        }

        public void InitializeStats(WeaponStats stats)
        {
            _stats = stats;
        }

        public void InitializeProjectilePool()
        {
            _pool = new ProjectilePool(this, 5);
            ProjectileParent = new();
            ProjectileParent.Name = "ProjectileParent";
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
                FireTimer.WaitTime = _stats.FireRate;
                GD.Print(
                    $"Setting FireTimer wait time from FireRate. FireRate = {_stats.FireRate} | WaitTime = {FireTimer.WaitTime}"
                );
            }
            AddChild(FireTimer);
        }

        public virtual void OnProjectileCollision(CollisionComponent collision) { }

        public virtual void Fire()
        {
            Projectile projectile = _pool.RequestProjectile();
            projectile.Position = ProjSpawnPoint;
        }
    }
}
