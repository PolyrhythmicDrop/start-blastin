using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autoloads;
using DataStructures;
using Enemies;
using Entities;
using Events;
using Godot;
using Interfaces;
using Projectiles;
using Stats;
using Utility;

namespace Weapons
{
    /// <summary>
    /// Node-based instantiation of a weapon. Weapons own a projectile pool and can have different firing behavior and stats.
    /// Weapons can be used by both enemies and the player.
    /// </summary>
    /// <remarks>
    /// WeaponNode objects are typically built in the <see cref="WeaponFactory"/> class using a base <see cref="WeaponResource"/> as its source of base stats.
    /// </remarks>
    [GlobalClass]
    public partial class WeaponNode : Node2D
    {
        private WeaponStats _stats;
        private ProjectilePool _pool;
        private IWeaponOwner _owner;
        private bool _enemyOwned;
        private bool _ownerSet;
        private int _activeProjectileCount;
        private IVelocityProvider _velocityProvider;

        /// <summary>
        /// The weapon's base stats, inherited from its parent <see cref="WeaponResource"/>.
        /// </summary>
        public WeaponStats Stats => _stats;

        /// <summary>
        /// Whether or not the weapon is owned by an enemy or a player.
        /// </summary>
        /// <remarks>
        /// This value can be set only once, when the weapon is built by the factory. The <see cref="_ownerSet"/> variable is set to true when <see cref="EnemyOwned"/> is set the first time.
        /// </remarks>
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

        /// <summary>
        /// The weapon's projectile pool. Initialized when the weapon is first created.
        /// </summary>
        public ProjectilePool Pool => _pool;

        /// <summary>
        /// The number of projectiles that are currently active.
        /// </summary>
        public int ActiveProjectileCount
        {
            get => _activeProjectileCount;
            set => _activeProjectileCount = value;
        }

        /// <summary>
        /// The position-less parent node of any projectiles fired from this weapon.
        /// Used so that each projectile's position is not dependent on the position of the weapon.
        /// </summary>
        public Node ProjectileParent;

        public BarrelRack Barrels = new();

        /// <summary>
        /// Timer used to re-trigger firing of the weapon when the "fire" button is held down.
        /// </summary>
        /// <remarks>
        /// The WaitTime of the FireTimer is set using the <see cref="WeaponStats.FireRate"/> of the weapon.
        /// </remarks>
        public Timer FireTimer;

        /// <summary>
        /// The weapon's velocity provider. Used to add a parent object's velocity to projectile speed.
        /// </summary>
        public IVelocityProvider VelocityProvider
        {
            get => _velocityProvider;
            set => _velocityProvider = value;
        }

        public IWeaponOwner WeaponOwner => _owner;

        /// <summary>
        /// Calls <see cref="InitializeProjectilePool"/> and <see cref="InitializeFireTimer"/>.
        /// </summary>
        public override void _Ready()
        {
            InitializeProjectilePool();
            InitializeFireTimer();
            SetBarrels();
        }

        /// <summary>
        /// Sets the weapon's <see cref="_stats"/> variable using dependency injection.
        /// </summary>
        /// <param name="stats">The WeaponStats to use as the base for this weapon's stats.</param>
        /// <remarks>
        /// This method is called from the <see cref="WeaponFactory"/> when the weapon is first created.
        /// </remarks>
        public void InitializeStats(WeaponStats stats)
        {
            _stats = stats;
        }

        public void SetOwner(IWeaponOwner owner)
        {
            _owner = owner;
            _ownerSet = true;
        }

        public void SetBarrels()
        {
            List<Node> children = [.. GetParent().GetChildren()];
            foreach (Barrel barrel in children.FindAll(child => child is Barrel))
            {
                Barrels.Add(barrel);
            }

            // Activate the first barrel by default
            Barrels.FirstOrDefault().ToggleActive(true);
        }

        /// <summary>
        /// Initializes the weapon's <see cref="ProjectilePool"/> by creating a new pool and projectile parent, then adding the projectile parent to the scene tree.
        /// </summary>
        public void InitializeProjectilePool()
        {
            _pool = new ProjectilePool(this, 5);
            ProjectileParent = new();
            ProjectileParent.Name = "ProjectileParent";
            AddChild(ProjectileParent);
        }

        public void ResetProjectilePool()
        {
            if (_pool != null)
            {
                _pool.ResetPool();
            }
            else
            {
                InitializeProjectilePool();
            }
        }

        /// <summary>
        /// Initializes the weapon's fire timer.
        /// Sets the WaitTime to the weapon's fire rate, then adds the FireTimer to the scene tree.
        /// </summary>
        public void InitializeFireTimer()
        {
            FireTimer = new();
            if (Stats == null)
            {
                DebugLogger.LogMessage(
                    $"Stats is null in {Name} before setting FireTimer.WaitTime!",
                    true,
                    true
                );
            }
            else
            {
                FireTimer.WaitTime = _stats.FireRate;
            }
            AddChild(FireTimer);
            FireTimer.Name = $"{Name}-FireTimer";
        }

        /// <summary>
        /// Checks if there are any active projectiles from this weapon.
        /// If there are still active projectiles, waits for the next process frame, then checks again.
        /// If none are found, returns true.
        /// </summary>
        /// <returns>True when <see cref="_activeProjectileCount"/> is 0.</returns>
        public async Task<bool> WaitForAllProjectilesDisabled()
        {
            // Find any active projectiles in the pool. If you find any, wait for the next frame. Else, return true.
            while (_activeProjectileCount != 0)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            return true;
        }

        public virtual void OnProjectileCollision(object source, CollisionEventArgs args)
        {
            if (source is not Projectile sourceProj)
            {
                return;
            }

            int playerId = -1;
            if (_owner is Player player)
            {
                playerId = player.PlayerId;
            }
            // IHealthful objects take damage.
            if (args.Collider is IHealthful healthful)
            {
                if (healthful is Player healthfulPlayer)
                {
                    if (!healthfulPlayer.Dodging)
                    {
                        healthfulPlayer.TakeDamage(_stats.Damage);
                        EventBus.Instance.RaisePlayerHitByProjectile(
                            healthfulPlayer.PlayerId,
                            sourceProj
                        );
                    }
                    else
                    {
                        return;
                    }
                }
                else if (healthful is EnemyNode enemy)
                {
                    enemy.TakeDamage(_stats.Damage, playerId);
                    EventBus.Instance.RaiseEnemyHit(playerId, enemy);
                }
            }

            // Projectiles deactivate.
            // TODO: Also add some kind of animation that plays.
            if (args.Collider is Projectile projectile)
            {
                projectile.ToggleActive(false);
            }

            // Deactivate the source projectile on collision.
            sourceProj.ToggleActive(false);
        }

        /// <summary>
        /// Fires the weapon.
        /// Can be overridden for custom firing behavior.
        /// </summary>
        public virtual void Fire()
        {
            // Fire from all active barrels.
            foreach (Barrel barrel in Barrels)
            {
                FireSingleBarrel(barrel);
            }

            if (EnemyOwned && !FireTimer.IsStopped())
            {
                FireTimer.Start(Stats.FireRate);
            }
        }

        public virtual void FireSingleBarrel(Barrel barrel)
        {
            if (barrel.Active == true)
            {
                Projectile projectile = _pool.RequestProjectile();
                projectile.Position = barrel.GlobalPosition;
                projectile.GlobalRotation = barrel.GlobalRotation;

                if (_velocityProvider != null)
                {
                    projectile.AddSourceVelocity();
                }
            }
        }

        public virtual void UpdateWeaponStats(StatType statType, Stat stat)
        {
            switch (statType)
            {
                case StatType.Damage:
                    _stats.Damage = stat.CurrentValue;
                    break;
                case StatType.FireRate:
                    _stats.FireRate = stat.CurrentValue;
                    break;
                case StatType.ProjectileSpeed:
                    _stats.ProjectileSpeed = stat.CurrentValue;
                    break;
                default:
                    break;
            }
        }
    }
}
