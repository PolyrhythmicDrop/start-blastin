using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Components;
using Godot;
using Interfaces;
using Projectiles;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponNode : Node2D
    {
        private WeaponStats _stats;
        private ProjectilePool _pool;
        private bool _enemyOwned;
        private bool _ownerSet;
        private int _activeProjectileCount;
        private bool _allProjectilesDisabledSignalEmitted;
        private IVelocityProvider _velocityProvider;

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
        public int ActiveProjectileCount
        {
            get => _activeProjectileCount;
            set => _activeProjectileCount = value;
        }

        public Node ProjectileParent;
        public virtual Vector2 ProjSpawnPoint => GlobalPosition;

        public Timer FireTimer;
        public Callable HitCallable;

        public IVelocityProvider VelocityProvider
        {
            get => _velocityProvider;
            set => _velocityProvider = value;
        }

        public WeaponNode()
        {
            HitCallable = Callable.From(
                (CollisionComponent collision) => OnProjectileCollision(collision)
            );
        }

        [Signal]
        public delegate void AllProjectilesDisabledEventHandler();

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
            }
            AddChild(FireTimer);
        }

        public async Task<bool> WaitForAllProjectilesDisabled()
        {
            // Find any active projectiles in the pool. If you find any, wait for the next frame. Else, return true.
            while (_pool.Find(proj => proj.Active) != null)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            return true;
        }

        public virtual void OnProjectileCollision(CollisionComponent collision)
        {
            if (collision.Collider is IHealthful healthful)
            {
                healthful.TakeDamage(_stats.Damage);
            }

            if (collision.Collider is Projectile projectile)
            {
                projectile.ToggleActive(false);
            }

            if (collision.Source is Projectile sourceProj)
            {
                sourceProj.ToggleActive(false);
            }
        }

        public virtual void Fire()
        {
            Projectile projectile = _pool.RequestProjectile();
            projectile.Position = ProjSpawnPoint;

            if (_velocityProvider != null)
            {
                projectile.AddSourceVelocity();
            }

            _allProjectilesDisabledSignalEmitted = false;
        }
    }
}
