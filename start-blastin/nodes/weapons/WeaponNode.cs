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
    /// WeaponNode objects are typically built in the <see cref="WeaponFactory"/> class using a base <see cref="WeaponStats"/> resource as its source of base stats.
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

        private readonly Dictionary<Barrel, ITetheredProjectile> _activeTethers = new();

        /// <summary>
        /// The weapon's base stats.
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

        // ~~ Timers ~~

        /// <summary>
        /// Timer used to re-trigger firing of the weapon when the "fire" button is held down.
        /// </summary>
        /// <remarks>
        /// The WaitTime of the FireTimer is set using the <see cref="WeaponStats.FireRate"/> of the weapon.
        /// </remarks>
        public Timer FireTimer;

        /// <summary>
        /// Timer used to set the time that firing is happening if this weapon has burst fire enabled.
        /// </summary>
        public Timer BurstFireTimer;

        /// <summary>
        /// Timer used to govern the time between burst fires, if burst firing is enabled.
        /// </summary>
        public Timer BurstCooldownTimer;

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
            // Get all the children of the parent node.
            List<Node> children = UtilityMethods.GetAllChildren(GetParent());

            foreach (Barrel barrel in children.FindAll(child => child is Barrel))
            {
                Barrels.Add(barrel);
                if (barrel.DefaultActive == true)
                {
                    barrel.ToggleActive(true);
                }
            }
        }

        /// <summary>
        /// Initializes the weapon's <see cref="ProjectilePool"/> by creating a new pool and projectile parent, then adding the projectile parent to the scene tree.
        /// </summary>
        public void InitializeProjectilePool()
        {
            _pool = new ProjectilePool(this, 5);
            ProjectileParent = new() { Name = "ProjectileParent" };
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
            try
            {
                if (_stats == null)
                {
                    throw new NullReferenceException(
                        $"Stats is null in {Name} before setting fire timer!"
                    );
                }

                FireTimer = new()
                {
                    WaitTime = _stats.FireRate,
                    Name = $"{Name}-FireTimer",
                    OneShot = true,
                    Autostart = false,
                };
                AddChild(FireTimer);

                // Burst fire
                if (_stats.BurstFire)
                {
                    if (_stats.BurstTime <= 0 || _stats.BurstCooldown <= 0)
                    {
                        throw new ArgumentException(
                            $"Burst time and burst cooldown must be greater than 0! Burst time: {_stats.BurstTime} | Burst cooldown: {_stats.BurstCooldown}!"
                        );
                    }

                    BurstFireTimer = new()
                    {
                        WaitTime = _stats.BurstTime,
                        OneShot = true,
                        Autostart = false,
                        Name = $"{Name}-BurstFireTimer",
                    };
                    AddChild(BurstFireTimer);

                    BurstCooldownTimer = new()
                    {
                        WaitTime = _stats.BurstCooldown,
                        OneShot = true,
                        Autostart = false,
                        Name = $"{Name}-BurstCooldownTimer",
                    };
                    AddChild(BurstCooldownTimer);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return;
            }
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

            // ** Deflection block **

            // If the source projectile is deflectable, see if we should be deflecting it
            if (sourceProj is DeflectableProjectile defSrcProj)
            {
                // If the collider has deflection active, deflect the projectile and return
                if (args.Collider is IDeflector deflector && deflector.DeflectActive)
                {
                    // Deflect and then return.
                    defSrcProj.Deflect(deflector, args);
                    return;
                }
            }

            // If the source projectile is a deflector, see if the colliding projectile is deflectable
            if (sourceProj is IDeflector srcDeflector && srcDeflector.DeflectActive)
            {
                // Deflect the collider if it's deflectable.
                if (args.Collider is DeflectableProjectile defColliderProj)
                {
                    // Invert the arguments
                    CollisionEventArgs invertedArgs = new(
                        sourceProj,
                        args.GlobalCollisionPoint,
                        args.CollisionNormal * -1
                    );
                    defColliderProj.Deflect(srcDeflector, invertedArgs);
                    return;
                }
            }

            // ** Damage **

            // IHealthful objects take damage.
            if (args.Collider is IHealthful healthful)
            {
                if (healthful is Player healthfulPlayer)
                {
                    if (!healthfulPlayer.State.Phasing)
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
            if (
                args.Collider is Projectile projectile
                && projectile is not ITetheredProjectile
                && projectile.DeactivateOnCollision
            )
            {
                projectile.ToggleActive(false);
            }

            // Deactivate the source projectile on collision if it's not tethered or has the flag set
            // TODO: Determine if we want to do this here or elsewhere, because some projectiles (Flame, Laser, etc.) might not want to deactivate on collision.
            if (sourceProj is not ITetheredProjectile && sourceProj.DeactivateOnCollision)
            {
                sourceProj.ToggleActive(false);
            }
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
                // Check for any active tethers on this barrel
                if (_activeTethers.TryGetValue(barrel, out ITetheredProjectile existingTether))
                {
                    // Return immediately if there's already an active tether on this barrel.
                    if (existingTether is Projectile proj && proj.Active)
                    {
                        return;
                    }
                    else
                    {
                        // If there's a tether on this barrel but the tether isn't active, that's a mistake and/or stale.
                        // Remove this entry from the dictionary.
                        _activeTethers.Remove(barrel);
                    }
                }

                Projectile projectile = _pool.RequestProjectile();
                projectile.Position = barrel.GlobalPosition;
                projectile.GlobalRotation = barrel.GlobalRotation;

                // Apply velocity.
                // TODO: Consider adding a condition that this not be a tethered projectile. Don't think I want to add velocity to those.
                if (_velocityProvider != null && projectile is not ITetheredProjectile)
                {
                    projectile.AddSourceVelocity(_velocityProvider.GetCurrentVelocity());
                }

                // If we're working with a tethered projectile, register it.
                if (projectile is ITetheredProjectile tethered)
                {
                    // Set the barrel and bool for the projectile
                    tethered.TetheredBarrel = barrel;
                    tethered.IsTethered = true;

                    // Register the barrel & projectile in the dictionary.
                    _activeTethers[barrel] = tethered;
                }
            }
        }

        /// <summary>
        /// Updates the relevant weapon stats based on an update to the owner's stats.
        /// </summary>
        /// <param name="statType">The stat type that was updated.</param>
        /// <param name="stat">The value of the stat to update.</param>
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

        /// <summary>
        /// Releases all currently active tethered projectiles.
        /// </summary>
        public void ReleaseAllTetheredProjectiles()
        {
            foreach (KeyValuePair<Barrel, ITetheredProjectile> kvp in _activeTethers)
            {
                if (kvp.Value is Projectile proj && proj.Active)
                {
                    // I release thee!
                    kvp.Value.ReleaseTether();
                }
            }

            _activeTethers.Clear();
        }

        /// <summary>
        /// Releases a tethered projectile from a single barrel.
        /// </summary>
        /// <param name="barrel"></param>
        public void ReleaseTetheredProjectile(Barrel barrel)
        {
            bool tethered = _activeTethers.TryGetValue(barrel, out ITetheredProjectile tether);

            if (!tethered)
            {
                return;
            }
            else
            {
                if (tether is Projectile proj && proj.Active)
                {
                    tether.ReleaseTether();
                }
                _activeTethers.Remove(barrel);
            }
        }
    }
}
