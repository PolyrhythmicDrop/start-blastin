using System;
using System.Threading.Tasks;
using Enemies;
using Factories;
using Godot;
using Stats;
using Utility;
using Weapons;

namespace Components
{
    /// <summary>
    /// Component for managing enemy weapons, including initialization, weapon stats, and weapon firing.
    /// </summary>
    public partial class EnemyWeaponComponent : Node
    {
        // The enemy that owns this component
        private EnemyNode _enemy;

        private bool _initialized = false;

        // Base stats (for scaling and effect resets)
        private float _baseFireRate;
        private float _baseWeaponDamage;
        public float BaseFireRate => _baseFireRate;
        public float BaseWeaponDamage => _baseWeaponDamage;

        private WeaponNode _weapon;

        /// <summary>
        /// The WeaponNode this component manages.
        /// </summary>
        public WeaponNode Weapon => _weapon;

        /// <summary>
        /// Is the weapon currently firing?
        /// </summary>
        public bool IsFiring => !_weapon.FireTimer.IsStopped();

        #region Init

        /// <summary>
        /// Initializes the weapon component. Called from <see cref="EnemyNode.Initialize(EnemyResource)"/>.
        /// </summary>
        /// <param name="enemy"></param>
        /// <param name="weaponStats"></param>
        public void Initialize(EnemyNode enemy, WeaponStats weaponStats)
        {
            // Set the owner.
            _enemy = enemy;

            // Set base stats.
            _baseFireRate = weaponStats.FireRate;
            _baseWeaponDamage = weaponStats.Damage;

            // Create the weapon using the factory.
            _weapon = WeaponFactory.CreateWeapon(
                weaponStats,
                velocityProvider: enemy,
                owner: enemy
            );

            // Set the initialization boolean
            _initialized = true;
        }

        /// <summary>
        /// Called after the weapon component and the enemy is initialized, when this weapon component enters the scene tree.
        /// Adds the weapon to the scene tree and connects the weapon timers.
        /// </summary>
        public override void _Ready()
        {
            if (!_initialized)
            {
                return;
            }

            // Weapon should still be the child of the enemy so we don't disrupt the scene tree hierarchy, barrel behavior, and positioning.
            _enemy.AddChild(_weapon);

            ConnectWeaponTimers();
        }

        /// <summary>
        /// Connects the callbacks for each weapon timer signal, including burst fire if applicable.
        /// </summary>
        private void ConnectWeaponTimers()
        {
            // Start the weapon fire timer to fire on a set interval.
            _weapon.FireTimer.Timeout += FireWeapon;

            if (_weapon.Stats.BurstFire)
            {
                _weapon.BurstFireTimer?.Timeout += OnBurstFireEnd;
                _weapon.BurstCooldownTimer?.Timeout += StartFiring;
            }
        }

        #endregion

        #region State and Stats

        /// <summary>
        /// Updates the correct weapon stat based on passed StatType and value.
        /// </summary>
        /// <param name="type">The type of stat to update.</param>
        /// <param name="stat">The Stat object whose values we'll set the stats to.</param>
        /// <returns>True if this is a weapon stat that we can update, false if not.</returns>
        public bool HandleStatUpdates(StatType type, Stat stat)
        {
            switch (type)
            {
                case StatType.FireRate:
                case StatType.Damage:
                case StatType.ProjectileSpeed:
                    _weapon.UpdateWeaponStats(type, stat);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Activates all the barrels for the managed weapon.
        /// </summary>
        public void ActivateAllBarrels()
        {
            _weapon.Barrels.ToggleActivateAllBarrels(true);
        }

        #endregion

        #region Firing

        /// <summary>
        /// Fires the managed weapon if the enemy is alive and on screen.
        /// Plays the associated fire sound and the fire animation.
        /// </summary>
        public void FireWeapon()
        {
            // Don't fire if we're dead or not on screen.
            if (!_enemy.IsAlive || !_enemy.VisibleNotifier.IsOnScreen())
            {
                return;
            }

            _enemy.AudioComp.PlayFireSound();
            _enemy.PlayFireAnimation();
            _weapon.Fire();
        }

        /// <summary>
        /// Begins firing the managed weapon.
        /// Calls <see cref="FireWeapon"/> once, then starts the relevant fire timers.
        /// Burst fire weapons start the <see cref="WeaponNode.BurstFireTimer"/> and <see cref="WeaponNode.FireTimer"/>.
        /// Regular weapons start the <see cref="WeaponNode.FireTimer"/>.
        /// </summary>
        public void StartFiring()
        {
            FireWeapon();

            if (_weapon.Stats.BurstFire)
            {
                _weapon.BurstFireTimer?.Start();
                _weapon.FireTimer.Start(_weapon.Stats.FireRate);
            }
            else
            {
                _weapon.FireTimer.OneShot = false;
                _weapon.FireTimer.Start(_weapon.Stats.FireRate);
            }
        }

        /// <summary>
        /// Stops all fire timers to stop firing the managed weapon.
        /// </summary>
        public void StopFiring()
        {
            _weapon.FireTimer.Stop();

            if (_weapon.Stats.BurstFire)
            {
                _weapon.BurstFireTimer?.Stop();
                _weapon.BurstCooldownTimer?.Stop();
            }
        }

        /// <summary>
        /// Called when the BurstFireTimer ends.
        /// Stops the <see cref="WeaponNode.FireTimer"/> and starts the <see cref="WeaponNode.BurstCooldownTimer"/>.
        /// When the <see cref="WeaponNode.BurstCooldownTimer"/> times out, <see cref="StartFiring"> is called in response.
        /// </summary>
        protected virtual void OnBurstFireEnd()
        {
            _weapon.FireTimer.Stop();
            _weapon.BurstCooldownTimer.Start(_weapon.Stats.BurstCooldown);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Wrapper method to release all tethered projectiles in the managed weapon.
        /// </summary>
        public void ReleaseAllTetheredProjectiles()
        {
            _weapon.ReleaseAllTetheredProjectiles();
        }

        /// <summary>
        /// Waits for all projectiles from this weapon to be disabled.
        /// </summary>
        public Task<bool> WaitForAllProjectilesDisabled()
        {
            return _weapon.WaitForAllProjectilesDisabled();
        }

        public override void _ExitTree()
        {
            if (_weapon != null)
            {
                _weapon.FireTimer.Timeout -= FireWeapon;

                if (_weapon.Stats.BurstFire)
                {
                    _weapon.BurstFireTimer.Timeout -= OnBurstFireEnd;
                    _weapon.BurstCooldownTimer.Timeout -= StartFiring;
                }
            }

            base._ExitTree();
        }
        #endregion
    }
}
