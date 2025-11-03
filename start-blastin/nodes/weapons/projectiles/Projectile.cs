using System;
using Components;
using Enemies;
using Godot;
using Utility;
using Weapons;

namespace Projectiles
{
    public abstract partial class Projectile : Area2D
    {
        private bool _sourceInitialized;
        private Callable _deactivateCallable;
        protected bool _active;
        protected Timer _deactivationTimer;
        protected float _baseSpeed;
        protected float _currentSpeed;
        protected WeaponNode _sourceWeapon;
        protected Vector2 _sourceVelocity => _sourceWeapon.VelocityProvider.GetCurrentVelocity();

        protected RayCast2D _ray;
        protected bool _rayInitialized = false;

        public RayCast2D Ray => _ray;

        /// <summary>
        /// Whether or not the projectile is currently active.
        /// An active projectile is in the scene tree and can be interacted with by other objects.
        /// </summary>
        public bool Active
        {
            get => _active;
            set => _active = value;
        }

        /// <summary>
        /// Time out for the projectile.
        /// Used as a failsafe to prevent memory leaks in case a projectile is somehow not freed when it should be.
        /// </summary>
        public Timer DeactivationTimer => _deactivationTimer;

        /// <summary>
        /// Base speed for the projectile. Set by the source weapon's parent resource.
        /// </summary>
        public virtual float BaseSpeed => _baseSpeed;

        /// <summary>
        /// Current speed of the projectile on this frame.
        /// </summary>
        public virtual float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// The weapon that this projectile belongs to.
        /// </summary>
        /// <remarks>
        /// This value can be set only once. Once set, it cannot be changed.
        /// </remarks>
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

        /// <summary>
        /// Signal that fires when this projectile collides with another object.
        /// </summary>
        /// <param name="collision">Information on the collision, including the collider, collision location, and source.</param>
        [Signal]
        public delegate void CollisionEventHandler(CollisionComponent collision);

        /// <summary>
        /// Constructor for the Projectile. Initializes the <see cref="DeactivationTimer"/> and the <see cref="_deactivateCallable"/>.
        /// </summary>
        public Projectile()
        {
            _deactivationTimer = new Timer();
            _deactivationTimer.WaitTime = 5;
            _deactivateCallable = Callable.From(() => ToggleActive(false));
        }

        /// <summary>
        /// Calls <see cref="InitializeStats"/>. Sets the rotation for the projectile based on the source weapon's rotation.
        /// </summary>
        public override void _Ready()
        {
            InitializeStats();
            _ray = GetNode<RayCast2D>("RayCast2D");

            if (_ray != null && !_rayInitialized)
            {
                InitializeRay();
            }
        }

        /// <summary>
        /// Initialize stats again every time the projectile enters the tree, since it can be added and removed from the tree by the ProjectilePool. We need to account for any change in the source's rotation to rotate the bullet successfully.
        /// </summary>
        public override void _EnterTree()
        {
            InitializeStats();
            // DebugLogger.LogMessage(
            //     $"Projectile added to scene tree! Base speed: {_baseSpeed} | Current speed: {_currentSpeed} | Source velocity: {_sourceVelocity} | Global rotation: {GlobalRotation}",
            //     true
            // );
            base._EnterTree();
        }

        protected virtual void InitializeRay()
        {
            if (SourceWeapon.EnemyOwned)
            {
                _ray.SetCollisionMaskValue(1, true);
                _ray.SetCollisionMaskValue(4, true);
                // _ray.SetCollisionMaskValue(3, false);
            }
            else
            {
                _ray.SetCollisionMaskValue(3, true);
                _ray.SetCollisionMaskValue(5, true);
            }

            _rayInitialized = true;
        }

        /// <summary>
        /// Sets the base speed to the source weapon's <see cref="WeaponStats.ProjectileSpeed"/> value.
        /// Initializes <see cref="_currentSpeed"/> to the new <see cref="_baseSpeed"/>.
        /// </summary>
        private void InitializeStats()
        {
            // Set the projectile's speed.
            // We need to do this here rather than in the constructor in case the player's weapon gets modified to increase projectile speed.
            _baseSpeed = _sourceWeapon.Stats.ProjectileSpeed;

            // Rotate the projectile to its parent's global rotation so that it fires in the direction the weapon is "facing".
            GlobalRotation = _sourceWeapon.GlobalRotation;
            _currentSpeed = _baseSpeed;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_active)
            {
                CastRay(delta);
                Position += SetTrajectory(delta);
            }
        }

        /// <summary>
        /// Enables or disables the <see cref="DeactivationTimer"/>.
        /// Connects or disconnects the timer's Timeout signal to the <see cref="_deactivateCallable"/>, which deactives the projectile on timeout.
        /// Starts or stops the DeactivationTimer.
        /// </summary>
        /// <param name="on">True to enable the deactivation timer, false to disable it.</param>
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

        /// <summary>
        /// Enables or disables the connection between the <see cref="Collision"/> signal and the source weapon's <see cref="WeaponNode.HitCallable"/> callback.
        /// </summary>
        /// <param name="connect">True to connect the signal, false to disconnect the signal.</param>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// Adds or removes the projectile from the scene tree, and increments or decrements the source weapon's active projectile count.
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

        /// <summary>
        /// Adds a fraction of the firing object's velocity to the projectile if the projectile is going in the same direction as the firing object.
        /// </summary>
        public virtual void AddSourceVelocity()
        {
            // Add speed in projectile's firing direction only
            float projectionMagnitude = _sourceVelocity.Dot(Vector2.Right.Rotated(GlobalRotation));

            // Only add a fraction of the movement speed to projectile speed.
            float extraVelocity = Mathf.Max(0, projectionMagnitude) * 0.6f;

            _currentSpeed = _baseSpeed + extraVelocity;
            // DebugLogger.LogMessage(
            //     $"Source velocity added! New current speed: {_currentSpeed} | Source velocity: {_sourceVelocity}",
            //     true
            // );
        }

        /// <summary>
        /// Casts a ray in the direction of movement to detect collisions and emit impact signals.
        /// </summary>
        /// <param name="delta">The physics frame delta time.</param>
        protected virtual void CastRay(double delta)
        {
            Vector2 nextPos = Position + SetTrajectory(delta);
            Ray.TargetPosition = ToLocal(nextPos);

            if (Ray.Enabled == false)
            {
                Ray.Enabled = true;
            }

            Ray.ForceRaycastUpdate();

            if (Ray.IsColliding())
            {
                CollisionComponent collision = new CollisionComponent()
                {
                    Source = this,
                    Collider = Ray.GetCollider(),
                    GlobalCollisionPoint = Ray.GetCollisionPoint(),
                    CollisionNormal = Ray.GetCollisionNormal() * -1,
                };

                EmitSignal(SignalName.Collision, collision);
            }
        }

        protected virtual Vector2 SetTrajectory(double delta)
        {
            Vector2 fireAngle = Vector2.Right.Rotated(GlobalRotation);

            // Logging for trajectory, since speedy drones don't seem to set it quite accurately.
            // if (_sourceWeapon.WeaponOwner is EnemyNode enemy)
            // {
            //     string enemyName = enemy.Name;
            //     if (enemyName.Contains("speedy-drone"))
            //     {
            //         DebugLogger.LogMessage(
            //             $"Tractory set! Fire angle: {fireAngle} | Projectile rotation: {GlobalRotation} | Source: {enemyName} | Source rotation: {enemy.GlobalRotation}"
            //         );
            //     }
            // }
            return _currentSpeed * (float)delta * fireAngle;
        }

        /// <summary>
        /// Called when the node exits the scene tree. Disconnects signals and disables the ray.
        /// </summary>
        public override void _ExitTree()
        {
            if (_ray != null && _ray.Enabled == true)
            {
                _ray.Enabled = false;
            }
            base._ExitTree();
        }
    }
}
