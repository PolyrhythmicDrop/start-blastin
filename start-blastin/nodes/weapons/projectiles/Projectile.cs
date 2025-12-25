using System;
using System.Threading.Tasks;
using Enemies;
using Entities;
using Events;
using Godot;
using Interfaces;
using Utility;
using Weapons;

namespace Projectiles
{
    public abstract partial class Projectile : Area2D
    {
        /// <summary>
        /// The group this projectile "belongs" to.
        /// </summary>
        public enum Faction
        {
            All,
            Players,
            Enemies,
            None,
        }

        private bool _sourceInitialized;
        private bool _factionInitialized;
        private Callable _deactivateCallable;
        protected bool _active;
        protected Faction _faction;
        protected Timer _deactivationTimer;
        protected float _baseSpeed;
        protected float _currentSpeed;
        protected WeaponNode _sourceWeapon;
        protected Vector2 _sourceVelocity => _sourceWeapon.VelocityProvider.GetCurrentVelocity();

        protected RayCast2D _ray;
        protected bool _rayInitialized = false;

        public RayCast2D Ray => _ray;

        public Faction CurrentFaction
        {
            get => _faction;
            set
            {
                if (!_factionInitialized)
                {
                    InitializeFaction(value);
                }
                else
                {
                    ConvertToNewFaction(value);
                }
            }
        }

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

        public event EventHandler<CollisionEventArgs> Collision;

        /// <summary>
        /// Constructor for the Projectile. Initializes the <see cref="DeactivationTimer"/> and the <see cref="_deactivateCallable"/>.
        /// </summary>
        public Projectile()
        {
            _deactivationTimer = new Timer();
            _deactivationTimer.WaitTime = 100;
            _deactivateCallable = Callable.From(() => ToggleActive(false));
        }

        #region Initialization

        /// <summary>
        /// Calls <see cref="InitializeStats"/>. Sets the rotation for the projectile based on the source weapon's rotation.
        /// </summary>
        public override void _Ready()
        {
            InitializeStats();
            _ray = GetNode<RayCast2D>("%TrajRayCast");

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
            base._EnterTree();
        }

        protected virtual void InitializeRay()
        {
            _ray.SetCollisionMaskValue(6, true);
            _ray.CollideWithAreas = true;

            if (_factionInitialized)
            {
                SetRayMask(_faction);
            }

            _rayInitialized = true;
        }

        /// <summary>
        /// Sets the collision masks for the targeting ray.
        /// </summary>
        /// <param name="faction">The Faction this projectile belongs to.</param>
        public void SetRayMask(Faction faction)
        {
            switch (faction)
            {
                case Faction.Enemies:
                {
                    _ray?.SetCollisionMaskValue(1, true);
                    _ray?.SetCollisionMaskValue(3, false);
                    _ray?.SetCollisionMaskValue(4, true);
                    _ray?.SetCollisionMaskValue(5, false);
                    break;
                }
                case Faction.Players:
                {
                    _ray?.SetCollisionMaskValue(1, false);
                    _ray?.SetCollisionMaskValue(3, true);
                    _ray?.SetCollisionMaskValue(4, false);
                    _ray?.SetCollisionMaskValue(5, true);
                    break;
                }
                case Faction.All:
                {
                    _ray?.SetCollisionMaskValue(1, true);
                    _ray?.SetCollisionMaskValue(4, true);
                    _ray?.SetCollisionMaskValue(3, true);
                    _ray?.SetCollisionMaskValue(5, true);
                    break;
                }
            }
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

        private void InitializeFaction(Faction faction)
        {
            _faction = faction;
            SetProjectileCollisionLayers(faction);
            _factionInitialized = true;
        }

        #endregion

        #region Processing
        public override void _PhysicsProcess(double delta)
        {
            if (_active)
            {
                CastRay(delta);
                Position += GetTrajectory(delta);
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
                Collision += _sourceWeapon.OnProjectileCollision;
            }
            else
            {
                Collision -= _sourceWeapon.OnProjectileCollision;
            }
        }

        /// <summary>
        /// Activates or deactivates the projectile in the source weapon's <see cref="ProjectilePool"/>.
        /// Adds or removes the projectile from the scene tree, and increments or decrements the source weapon's active projectile count.
        /// </summary>
        /// <param name="active">True to activate the projectile. False to deactivate the projectile.</param>
        public virtual void ToggleActive(bool active)
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
        #endregion

        #region Physics

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
        }

        public virtual void AddSourceVelocity(Vector2 velocity)
        {
            // Add speed in projectile's firing direction only
            float projectionMagnitude = velocity.Dot(Vector2.Right.Rotated(GlobalRotation));

            // Only add a fraction of the movement speed to projectile speed.
            float extraVelocity = Mathf.Max(0, projectionMagnitude) * 0.6f;

            _currentSpeed = _baseSpeed + extraVelocity;
        }

        public virtual void AddDeflectionVelocity(Vector2 velocity)
        {
            float magnitude = velocity.Dot(Vector2.Right.Rotated(GlobalRotation));

            float extraVelocity = Mathf.Max(0, magnitude);

            _currentSpeed += extraVelocity;
        }

        /// <summary>
        /// Casts a ray in the direction of movement to detect collisions and emit impact signals.
        /// </summary>
        /// <param name="delta">The physics frame delta time.</param>
        protected virtual void CastRay(double delta)
        {
            Vector2 nextPos = GlobalPosition + GetTrajectory(delta);
            Ray.TargetPosition = ToLocal(nextPos);

            if (Ray.Enabled == false)
            {
                Ray.Enabled = true;
            }

            Ray.ForceRaycastUpdate();

            if (Ray.IsColliding())
            {
                // Vector2 collisionNormal = Ray.GetCollisionNormal();
                // CollisionEventArgs collision = new(
                //     Ray.GetCollider(),
                //     Ray.GetCollisionPoint(),
                //     collisionNormal == Vector2.Zero ? Vector2.Zero : collisionNormal * -1
                // );

                Collision?.Invoke(this, CalculateCollisionData(delta));
            }
        }

        protected virtual CollisionEventArgs CalculateCollisionData(double delta)
        {
            Vector2 collNormal = Ray.GetCollisionNormal();
            Vector2 collPoint = Ray.GetCollisionPoint();
            GodotObject collider = Ray.GetCollider();

            // If we get a 0 normal (likely because the ray started inside the collider), calculate the normal manually.
            if (collNormal == Vector2.Zero)
            {
                // Calculate normal from relative position for a Node2D
                if (collider is Node2D collNode)
                {
                    // Get the direction from the center of the collider to the Projectile.
                    Vector2 collDir = (GlobalPosition - collNode.GlobalPosition).Normalized();
                    collNormal = collDir;
                }
                else
                {
                    // Otherwise, just use the opposite direction of the projectile
                    collNormal = Vector2.Right.Rotated(GlobalRotation).Normalized() * -1;
                }
            }
            else
            {
                collNormal *= -1;
            }

            return new CollisionEventArgs(collider, collPoint, collNormal);
        }

        protected virtual Vector2 GetTrajectory(double delta)
        {
            if (Mathf.Sign(GlobalRotation) == -1)
            {
                GlobalRotation = UtilityMethods.ConvertNegativeRotationRads(GlobalRotation);
            }
            Vector2 fireVector = Vector2.Right.Rotated(GlobalRotation).Normalized();
            return _currentSpeed * (float)delta * fireVector;
        }

        #endregion

        #region Collision

        public void SetProjectileCollisionLayers(Faction faction)
        {
            // Enable aura detection (Layer 6) for both types
            SetCollisionMaskValue(6, true);

            switch (faction)
            {
                case Faction.Enemies:
                {
                    // Set the mask so the projectile hits players.
                    SetCollisionMaskValue(1, true);
                    // Set the mask so that the projectile does not hit fellow enemies.
                    SetCollisionMaskValue(3, false);
                    // Set collision layer 4 (Projectiles-Player) to false.
                    SetCollisionLayerValue(4, false);
                    // Set the mask so the projectile hits player projectiles.
                    SetCollisionMaskValue(4, true);
                    // Set the collision layer 5 (Projectiles-Enemy) to true.
                    SetCollisionLayerValue(5, true);
                    // Set the mask so the projectile does not hit other enemy projectiles.
                    SetCollisionMaskValue(5, false);
                    break;
                }
                case Faction.Players:
                {
                    // Set the mask so the projectile hits players.
                    SetCollisionMaskValue(1, true);
                    // Set the mask so that the projectile does not hit fellow enemies.
                    SetCollisionMaskValue(3, false);
                    // Set collision layer 4 (Projectiles-Player) to false.
                    SetCollisionLayerValue(4, false);
                    // Set the mask so the projectile hits player projectiles.
                    SetCollisionMaskValue(4, true);
                    // Set the collision layer 5 (Projectiles-Enemy) to true.
                    SetCollisionLayerValue(5, true);
                    // Set the mask so the projectile does not hit other enemy projectiles.
                    SetCollisionMaskValue(5, false);
                    break;
                }
                case Faction.All:
                {
                    // Set all relevant masks and layers to true.
                    SetCollisionMaskValue(1, true);
                    SetCollisionMaskValue(3, true);
                    SetCollisionLayerValue(4, true);
                    SetCollisionMaskValue(4, true);
                    SetCollisionLayerValue(5, true);
                    SetCollisionMaskValue(5, true);
                    break;
                }
            }
        }

        /// <summary>
        /// Converts the projectile to a new owner, either the player or an enemy.
        /// Affects the shader material and the collision layers.
        /// </summary>
        public virtual void ConvertToNewFaction(Faction? faction = null)
        {
            Faction newFaction;

            // If no Faction value is passed, swap the Faction to the opposite
            if (faction.HasValue)
            {
                newFaction = faction.Value;
            }
            else
            {
                newFaction = _faction switch
                {
                    Faction.Players => Faction.Enemies,
                    Faction.Enemies => Faction.Players,
                    Faction.All => Faction.None,
                    Faction.None => Faction.All,
                    _ => Faction.All,
                };
            }

            // Set the collision layers for the projectile and its RayCast
            SetProjectileCollisionLayers(newFaction);
            SetRayMask(newFaction);

            _faction = newFaction;

            // If this new faction is different from the initially-set faction...
            if (_factionInitialized)
            {
                // Remove the projectile from the pool so it doesn't get re-used in its new role.
                SourceWeapon.Pool.Remove(this);
            }
        }

        public virtual void Deflect(IDeflector deflector, CollisionEventArgs args = null)
        {
            // Convert to the opposite faction of the current faction.
            ConvertToNewFaction();

            // Default naive deflection, 180deg from current rotation.
            if (args == null)
            {
                GlobalRotation += MathF.PI;
            }
            // If we get a normal and some more advanced args, perform a bounce
            else
            {
                // Get the current direction vector based on rotation
                Vector2 currentDir = Vector2.Right.Rotated(GlobalRotation).Normalized();

                // Bounce the direction vector off the collision's normal
                Vector2 bounceDir = currentDir.Bounce(args.CollisionNormal);

                // Convert the bounced direction to rotation.
                GlobalRotation = bounceDir.Angle();
            }

            if (deflector is IVelocityProvider velocitySource)
            {
                AddDeflectionVelocity(velocitySource.GetCurrentVelocity());
            }
        }

        public void SetProjectileAuraDetection(bool areaDetect)
        {
            if (areaDetect)
            {
                _ray.SetCollisionMaskValue(6, true);
                // _ray.CollideWithAreas = true;
                SetCollisionMaskValue(6, true);
            }
            else
            {
                _ray.SetCollisionMaskValue(6, false);
                // _ray.CollideWithAreas = false;
                SetCollisionMaskValue(6, false);
            }
        }

        #endregion

        #region Cleanup
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
        #endregion
    }
}
