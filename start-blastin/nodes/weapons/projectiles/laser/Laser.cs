using Environmental;
using Godot;
using Interfaces;
using Utility;
using Weapons;

namespace Projectiles
{
    [GlobalClass]
    public partial class Laser : Projectile, ITetheredProjectile
    {
        private Line2D _bodyLine;
        private Line2D _boltLine;

        // Shader materials and tweening callback functions
        private ShaderMaterial _bodyLineShaderMat;
        private ShaderMaterial _boltLineShaderMat;
        private Callable _bodyLineAlphaModCall;
        private Callable _boltLineAlphaModCall;

        private Color _baseLineMod;
        private Color _transMod;

        private GpuParticles2D _barrelParticles;
        private GpuParticles2D _impactParticles;
        private GpuParticles2D _bodyParticles;

        private bool _impactParticlesActive;

        private Tween _laserTween;

        public Barrel TetheredBarrel { get; set; }

        public bool IsTethered { get; set; }

        /// <summary>
        /// Maximum length of the laser.
        /// </summary>
        private const float MAX_LENGTH = 2000;

        /// <summary>
        /// The distance the <see cref="_bodyLine"/> should start from the Laser's base Position.
        /// </summary>
        private const float START_DISTANCE = 30;

        /// <summary>
        /// The offset of the <see cref="_boltLine"/> from the start and end of the <see cref="_bodyLine"/>.
        /// </summary>
        private const float BOLT_OFFSET = 20;

        /// <summary>
        /// Fade duration for the laser's tweens.
        /// </summary>
        private const float FADE_DURATION = 0.2f;

        #region Init

        public override void _Ready()
        {
            base._Ready();
            _ignoreOtherProjectiles = true;

            // Set up the Line2Ds.
            _bodyLine = GetNode<Line2D>("%BodyLine");
            _boltLine = GetNode<Line2D>("%BoltLine");
            _bodyLine.Visible = false;
            _boltLine.Visible = false;
            InitLinePoints();

            // Set up the modulation variables.
            _baseLineMod = _bodyLine.Modulate;
            _transMod = new(_baseLineMod.R, _baseLineMod.G, _baseLineMod.B, 0);

            // Set the shader materials and the callbacks for later tweening.
            _bodyLineShaderMat = (ShaderMaterial)_bodyLine.Material;
            _boltLineShaderMat = (ShaderMaterial)_boltLine.Material;
            _bodyLineAlphaModCall = Callable.From(
                (float value) =>
                {
                    _bodyLineShaderMat.SetShaderParameter("alpha_mod", value);
                }
            );
            _boltLineAlphaModCall = Callable.From(
                (float value) =>
                {
                    _boltLineShaderMat.SetShaderParameter("alpha_mod", value);
                }
            );

            // Set up the barrel particles
            _barrelParticles = GetNode<GpuParticles2D>("%BarrelParticles");
            _barrelParticles.Position = new(START_DISTANCE / 2, 0);
            _barrelParticles.Emitting = false;

            // Set up the body particles
            _bodyParticles = GetNode<GpuParticles2D>("%BodyParticles");

            // Set up the impact particles
            _impactParticles = GetNode<GpuParticles2D>("%ImpactParticles");
            _impactParticles.Emitting = false;
        }

        /// <summary>
        /// Resets the start and end points of both <see cref="_bodyLine"/> and <see cref="_boltLine"/> to their default values.
        /// </summary>
        public void InitLinePoints()
        {
            _bodyLine.ClearPoints();
            _bodyLine.AddPoint(new(START_DISTANCE, 0));
            _bodyLine.AddPoint(Vector2.Zero);

            _boltLine.ClearPoints();
            _boltLine.AddPoint(new(START_DISTANCE + BOLT_OFFSET, 0));
            _boltLine.AddPoint(Vector2.Zero);
        }

        #endregion

        #region Activation

        public override async void ToggleActive(bool active)
        {
            if (active)
            {
                // Normal projectile parenting
                _sourceWeapon.ProjectileParent.AddChild(this);
                _sourceWeapon.ActiveProjectileCount++;

                _barrelParticles.Modulate = _baseLineMod;
                _barrelParticles.Restart();

                _bodyParticles.Modulate = _baseLineMod;
                _bodyParticles.Restart();

                _bodyLine.Visible = true;
                _boltLine.Visible = true;

                TweenActivation();

                // Weapon will handle assigning a barrel and changing _isTethered, don't do it here.
            }
            else
            {
                TweenDeactivation();

                if (_laserTween != null)
                {
                    await ToSignal(_laserTween, Tween.SignalName.Finished);
                }

                // Reset the points in the line.
                InitLinePoints();
                // Reset the ray's target position.
                Ray.TargetPosition = Vector2.Zero;

                // Reset body emission box to zero
                UpdateLaser(Vector2.Zero);

                _bodyLine.Visible = false;
                _boltLine.Visible = false;

                // Normal projectile de-parenting
                _sourceWeapon.ProjectileParent.RemoveChild(this);
                _sourceWeapon.ActiveProjectileCount--;

                // Remove barrel assignment and tethering bool
                IsTethered = false;
                TetheredBarrel = null;

                // Reset the ray target position
            }

            _active = active;
            ToggleCollisionSignalConnection(active);

            // No need to toggle the deactivation timer for tethered projectiles.
        }

        /// <summary>
        /// Fades in the body and bolt Line2D's using the <see cref="_laserTween"/>.
        /// </summary>
        private void TweenActivation()
        {
            if (_laserTween != null && _laserTween.IsValid())
            {
                _laserTween.Kill();
            }

            var currentAlpha = _bodyLineShaderMat.GetShaderParameter("alpha_mod");

            _laserTween = CreateTween().SetParallel();
            _laserTween.TweenMethod(_bodyLineAlphaModCall, currentAlpha, 1.0, FADE_DURATION);
            _laserTween.TweenMethod(_boltLineAlphaModCall, 0, 1.0, FADE_DURATION);
        }

        /// <summary>
        /// Fades out the body and bolt Line2D's using the <see cref="_laserTween"/>.
        /// Fades out the barrel and body particles before setting both particle nodes' Emitting value to false.
        /// </summary>
        private void TweenDeactivation()
        {
            if (_laserTween != null && _laserTween.IsValid())
            {
                _laserTween.Kill();
            }

            _laserTween = CreateTween().SetParallel();
            _laserTween.TweenMethod(_bodyLineAlphaModCall, 1.0, 0, FADE_DURATION);
            _laserTween.TweenMethod(_boltLineAlphaModCall, 1.0, 0, FADE_DURATION);
            _laserTween.TweenProperty(_barrelParticles, "modulate", _transMod, FADE_DURATION);
            _laserTween.TweenProperty(_bodyParticles, "modulate", _transMod, FADE_DURATION);
            _laserTween.SetParallel(false);
            _laserTween.TweenProperty(_barrelParticles, "emitting", false, 0);
            _laserTween.TweenProperty(_bodyParticles, "emitting", false, 0);
        }

        #endregion

        #region Process

        /// <summary>
        /// Expands the laser based on the projectile speed and delta by calling <see cref="CastRay"/> and using the new target position to set the line end point.
        /// </summary>
        /// <param name="delta">The time between frames.</param>
        public override void _PhysicsProcess(double delta)
        {
            if (_active)
            {
                CastRay(delta);
            }
        }

        /// <summary>
        /// Calls <see cref="UpdateTether"/> every frame if this Laser is active.
        /// </summary>
        /// <param name="delta"></param>
        public override void _Process(double delta)
        {
            if (_active)
            {
                base._Process(delta);
                UpdateTether();
            }
        }

        /// <summary>
        /// Sets the ray's target position based on the speed of the projectile and the current speed.
        /// Clamps the target position to the viewport so the laser does not extend beyond the bounds of the viewport.
        /// Sets the position of the end of the laser using the ray's target position.
        /// At the end, updates the laser graphics and particles based on the laser end point and whether or not the laser is colliding with something.
        /// </summary>
        /// <param name="delta"></param>
        protected override void CastRay(double delta)
        {
            if (Ray.Enabled == false)
            {
                Ray.Enabled = true;
            }

            Rect2 viewRect = GetViewport().GetVisibleRect();

            Ray.TargetPosition = new(
                (float)Mathf.MoveToward(Ray.TargetPosition.X, MAX_LENGTH, _currentSpeed * delta),
                0
            );

            // Convert target position to global space so we can see if it's in the viewport.
            Vector2 tpGlobal = ToGlobal(Ray.TargetPosition);
            // Clamp the target position to the viewport.
            Ray.TargetPosition = ToLocal(
                tpGlobal.Clamp(
                    viewRect.Position + new Vector2(-50, -50),
                    viewRect.End + new Vector2(50, 50)
                )
            );

            // Set the initial end point for the laser
            Vector2 laserEnd = Ray.TargetPosition;

            Ray.ForceRaycastUpdate();

            bool colliding = Ray.IsColliding();
            if (colliding)
            {
                GodotObject collider = Ray.GetCollider();
                if (
                    collider
                    is not (Projectile { IgnoreOtherProjectiles: true } or OobArea or DeflectorWall)
                )
                {
                    RaiseCollision(this, CalculateRayCollisionData(delta, collider));
                    laserEnd = ToLocal(Ray.GetCollisionPoint());
                    Ray.TargetPosition = ToLocal(Ray.GetCollisionPoint());
                }
            }

            UpdateLaser(laserEnd);
            UpdateImpactParticles(laserEnd, colliding);
        }

        /// <summary>
        /// Makes the <see cref="_impactParticles"/> visible or invisible, based on whether or not the laser is colliding with something.
        /// If the laser is colliding, updates the impact particles' position.
        /// </summary>
        /// <param name="pos">The position of the impact particles node. Only used if <paramref name="colliding"/> is true.</param>
        /// <param name="colliding">Whether or not the laser is colliding with an object.</param>
        private void UpdateImpactParticles(Vector2 pos, bool colliding)
        {
            if (colliding)
            {
                if (!_impactParticlesActive)
                {
                    _impactParticlesActive = true;

                    _impactParticles.Restart();
                    _impactParticles.Modulate = _baseLineMod;
                    _impactParticles.Visible = true;
                }

                _impactParticles.Position = pos;
            }
            else
            {
                if (_impactParticlesActive)
                {
                    _impactParticlesActive = false;
                    _impactParticles.Visible = false;
                    _impactParticles.Emitting = false;
                }
            }
        }

        /// <summary>
        /// Updates the end point of the Line2D that is the visual representation of the laser.
        /// </summary>
        /// <param name="endPoint">The end point of the laser. This argument should be in local coordinates.</param>
        private void UpdateLaser(Vector2 endPoint)
        {
            int pCount = _bodyLine.GetPointCount();
            _bodyLine.SetPointPosition(pCount - 1, endPoint);

            Vector2 boltPoint = new(endPoint.X - BOLT_OFFSET, endPoint.Y);

            int pBCount = _boltLine.GetPointCount();
            _boltLine.SetPointPosition(pBCount - 1, boltPoint);

            // Set the body particles
            Vector2 startPoint = _bodyLine.Points[0];
            _bodyParticles.Position = startPoint + (endPoint - startPoint) * 0.5f;
            ParticleProcessMaterial pm = (ParticleProcessMaterial)_bodyParticles.ProcessMaterial;
            pm.EmissionBoxExtents = new(
                endPoint.DistanceTo(startPoint) * 0.5f,
                pm.EmissionBoxExtents.Y,
                pm.EmissionBoxExtents.Z
            );
        }

        #endregion

        #region Tethering

        public void ReleaseTether()
        {
            IsTethered = false;
            ToggleActive(false);
        }

        public void UpdateTether()
        {
            // Keep the laser fixed to the barrel
            if (TetheredBarrel != null && _active)
            {
                GlobalPosition = TetheredBarrel.GlobalPosition;
                GlobalRotation = TetheredBarrel.GlobalRotation;
            }
        }

        #endregion
    }
}
