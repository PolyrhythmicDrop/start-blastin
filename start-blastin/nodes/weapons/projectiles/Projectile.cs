using System;
using Components;
using Godot;
using Weapons;

namespace Projectiles
{
    public abstract partial class Projectile : Node2D
    {
        private bool _sourceInitialized;
        protected bool _active;
        protected Timer _deactivationTimer;
        protected float _speed;
        protected WeaponNode _sourceWeapon;

        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        public Timer DeactivationTimer => _deactivationTimer;

        public virtual float Speed => _speed;

        internal WeaponNode SourceWeapon
        {
            get => _sourceWeapon;
            set
            {
                if (_sourceInitialized)
                {
                    throw new InvalidOperationException(
                        $"{Name}: SourceWeapon can only be set once during initialization!"
                    );
                }
                else
                {
                    _sourceWeapon = value;
                    _sourceInitialized = true;
                }
            }
        }

        [Signal]
        public delegate void CollisionEventHandler(CollisionComponent collision);

        public Projectile()
        {
            _deactivationTimer = new Timer();
            _deactivationTimer.WaitTime = 5;
        }

        public override void _Ready()
        {
            SetDeactivationTimer();
            Initialize();
        }

        private void Initialize()
        {
            _speed = _sourceWeapon.Stats.ProjSpeed;
        }

        private void SetDeactivationTimer()
        {
            if (!IsAncestorOf(_deactivationTimer))
            {
                AddChild(_deactivationTimer);
            }
            _deactivationTimer.Timeout += () => ToggleActive(false);
            _deactivationTimer.Start();
        }

        /// <summary>
        /// Activates or deactivates the projectile in the source weapon's <see cref="ProjectilePool"/>.
        /// </summary>
        /// <param name="active">True to activate the projectile. False to deactivate the projectile.</param>
        public void ToggleActive(bool active)
        {
            if (active)
            {
                _sourceWeapon.Pool.ActivateProjectile(this);
            }
            else
            {
                _sourceWeapon.Pool.DeactivateProjectile(this);
            }
        }
    }
}
