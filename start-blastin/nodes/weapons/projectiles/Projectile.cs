using System;
using Components;
using Godot;
using Weapons;

namespace Projectiles
{
    public abstract partial class Projectile : Area2D
    {
        private bool _sourceInitialized;
        private Callable _deactivateCallable;

        // protected Area2D _area;
        protected bool _active;
        protected Timer _deactivationTimer;
        protected float _baseSpeed;
        protected float _currentSpeed;
        protected WeaponNode _sourceWeapon;
        protected Vector2 _sourceVelocity => _sourceWeapon.VelocityProvider.GetCurrentVelocity();

        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        public Timer DeactivationTimer => _deactivationTimer;

        public virtual float BaseSpeed => _baseSpeed;
        public virtual float CurrentSpeed => _currentSpeed;

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
            _deactivateCallable = Callable.From(() => ToggleActive(false));
        }

        public override void _Ready()
        {
            // _area = GetNode<Area2D>("%Area2D");
            Initialize();
            // Rotate the projectile to its parent's global rotation so that it fires in the direction the weapon is "facing".
            GlobalRotation = _sourceWeapon.GlobalRotation;
        }

        /// <summary>
        /// Called as part of ready, when the Projectile is added to the scene tree.
        /// </summary>
        private void Initialize()
        {
            // Set the projectile's speed.
            // We need to do this here rather than in the constructor in case the player's weapon gets modified to increase projectile speed.
            _baseSpeed = _sourceWeapon.Stats.ProjSpeed;
            _currentSpeed = _baseSpeed;
        }

        private void ToggleDeactivationTimer(bool on)
        {
            if (!IsAncestorOf(_deactivationTimer))
            {
                AddChild(_deactivationTimer);
            }
            if (on)
            {
                if (!_deactivationTimer.IsConnected(Timer.SignalName.Timeout, _deactivateCallable))
                {
                    _deactivationTimer.Connect(Timer.SignalName.Timeout, _deactivateCallable);
                }
                _deactivationTimer.Start();
            }
            else
            {
                if (_deactivationTimer.IsConnected(Timer.SignalName.Timeout, _deactivateCallable))
                {
                    _deactivationTimer.Disconnect(Timer.SignalName.Timeout, _deactivateCallable);
                }
                _deactivationTimer.Stop();
            }
        }

        private void ToggleCollisionSignalConnection(bool connect)
        {
            if (connect)
            {
                if (!IsConnected(SignalName.Collision, _sourceWeapon.HitCallable))
                {
                    Connect(SignalName.Collision, _sourceWeapon.HitCallable, 4);
                }
                else
                {
                    throw new InvalidOperationException(
                        Name + " is already connected to " + SignalName.Collision
                    );
                }
            }
            else
            {
                if (IsConnected(SignalName.Collision, _sourceWeapon.HitCallable))
                {
                    Disconnect(SignalName.Collision, _sourceWeapon.HitCallable);
                }
            }
        }

        /// <summary>
        /// Activates or deactivates the projectile in the source weapon's <see cref="ProjectilePool"/>.
        /// </summary>
        /// <param name="active">True to activate the projectile. False to deactivate the projectile.</param>
        public void ToggleActive(bool active)
        {
            if (active)
            {
                _sourceWeapon.ProjectileParent.AddChild(this);
                _sourceWeapon.ActiveProjectileCount++;
            }
            else
            {
                // Add items from projectile pool
                _sourceWeapon.ProjectileParent.RemoveChild(this);
                _sourceWeapon.ActiveProjectileCount--;
            }

            _active = active;
            ToggleDeactivationTimer(active);
            ToggleCollisionSignalConnection(active);
        }

        public virtual void AddSourceVelocity()
        {
            // Add speed in projectile's firing direction only
            float projectionMagnitude = _sourceVelocity.Dot(Vector2.Right.Rotated(GlobalRotation));
            _currentSpeed = _baseSpeed + Mathf.Max(0, projectionMagnitude);
        }
    }
}
