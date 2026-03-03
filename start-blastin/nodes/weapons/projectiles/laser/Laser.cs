using System;
using System.Threading.Tasks;
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

        private GpuParticles2D _barrelParticles;
        private GpuParticles2D _impactParticles;
        private GpuParticles2D _bodyParticles;

        private bool _impactParticlesActive;

        private Tween _laserTween;
        private Tween _impactTween;

        private Barrel _tBarrel;

        public Barrel TetheredBarrel
        {
            get => _tBarrel;
            set => _tBarrel = value;
        }

        private bool _tethered;
        public bool IsTethered
        {
            get => _tethered;
            set => _tethered = value;
        }

        /// <summary>
        /// Maximum length of the laser. Set to be outside the screen bounds no matter where you fire it from or in what direction.
        /// </summary>
        private float _maxLength = 1080;

        private const float START_DISTANCE = 30;

        private const float BOLT_OFFSET = 20;

        private const float FADE_DURATION = 0.2f;

        public override void _Ready()
        {
            base._Ready();
            _ignoreOtherProjectiles = true;

            _bodyLine = GetNode<Line2D>("%BodyLine");
            _boltLine = GetNode<Line2D>("%BoltLine");
            _baseLineMod = _bodyLine.Modulate;

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

            _barrelParticles = GetNode<GpuParticles2D>("%BarrelParticles");
            _barrelParticles.Position = new(START_DISTANCE / 2, 0);
            _barrelParticles.Emitting = false;

            _bodyParticles = GetNode<GpuParticles2D>("%BodyParticles");

            _impactParticles = GetNode<GpuParticles2D>("%ImpactParticles");
            // _impactParticles.Visible = false;
            _impactParticles.Emitting = false;
            // ParticleProcessMaterial impProcessMat = (ParticleProcessMaterial)
            //     _impactParticles.ProcessMaterial;
            // impProcessMat.EmissionBoxExtents = new Vector3(1, _bodyLine.Width / 2, 1);
            // impProcessMat.EmissionSphereRadius = _bodyLine.Width / 2;

            _bodyLine.Visible = false;
            _boltLine.Visible = false;
            InitLinePoints();
        }

        public void InitLinePoints()
        {
            _bodyLine.ClearPoints();
            _bodyLine.AddPoint(new(START_DISTANCE, 0));
            _bodyLine.AddPoint(Vector2.Zero);

            _boltLine.ClearPoints();
            _boltLine.AddPoint(new(START_DISTANCE + BOLT_OFFSET, 0));
            _boltLine.AddPoint(Vector2.Zero);
        }

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
                DeactivateImpactParticles();
                TweenDeactivation();

                if (_laserTween != null)
                {
                    await ToSignal(_laserTween, Tween.SignalName.Finished);
                }

                // Reset body emission box to zero
                UpdateLaserEndPoint(Vector2.Zero);

                _bodyLine.Visible = false;
                _boltLine.Visible = false;

                // Normal projectile de-parenting
                _sourceWeapon.ProjectileParent.RemoveChild(this);
                _sourceWeapon.ActiveProjectileCount--;

                // Remove barrel assignment and tethering bool
                _tethered = false;
                _tBarrel = null;

                // Reset the points in the line.
                InitLinePoints();

                // Reset the ray target position
                Ray.TargetPosition = Vector2.Zero;
            }

            _active = active;
            ToggleCollisionSignalConnection(active);

            // No need to toggle the deactivation timer for tethered projectiles.
        }

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

        private void TweenDeactivation()
        {
            if (_laserTween != null && _laserTween.IsValid())
            {
                _laserTween.Kill();
            }

            // Create the transparent modulation color.
            Color trans = _baseLineMod;
            trans.A = 0;

            _laserTween = CreateTween().SetParallel();
            _laserTween.TweenMethod(_bodyLineAlphaModCall, 1.0, 0, FADE_DURATION);
            _laserTween.TweenMethod(_boltLineAlphaModCall, 1.0, 0, FADE_DURATION);
            _laserTween.TweenProperty(_barrelParticles, "modulate", trans, FADE_DURATION);
            _laserTween.TweenProperty(_bodyParticles, "modulate", trans, FADE_DURATION);
            _laserTween.SetParallel(false);
            _laserTween.TweenProperty(_barrelParticles, "emitting", false, 0);
            _laserTween.TweenProperty(_bodyParticles, "emitting", false, 0);
        }

        /// <summary>
        /// Expands the laser based on the projectile speed and delta by calling CastRay() and using the new target position to set the line end point.
        /// Calls <see cref="UpdateTether"/> every frame if this Laser is active.
        /// </summary>
        /// <param name="delta">The time between frames.</param>
        public override void _PhysicsProcess(double delta)
        {
            if (_active)
            {
                CastRay(delta);
            }
        }

        public override void _Process(double delta)
        {
            if (_active)
            {
                base._Process(delta);
                UpdateTether();
            }
        }

        protected override void CastRay(double delta)
        {
            if (Ray.Enabled == false)
            {
                Ray.Enabled = true;
            }

            Rect2 viewRect = GetViewport().GetVisibleRect();

            Ray.TargetPosition = new(
                (float)Mathf.MoveToward(Ray.TargetPosition.X, _maxLength, _currentSpeed * delta),
                0
            );

            // Convert target position to global space so we can see if it's in the viewport.
            Vector2 tpGlobal = ToGlobal(Ray.TargetPosition);
            // Clamp the target position to the viewport.
            Ray.TargetPosition = ToLocal(tpGlobal.Clamp(viewRect.Position, viewRect.End));

            // Set the initial end point for the laser
            Vector2 laserEnd = Ray.TargetPosition;

            // DebugLogger.LogMessage($"Ray target position: {Ray.TargetPosition}");

            Ray.ForceRaycastUpdate();

            bool colliding = Ray.IsColliding();
            if (colliding)
            {
                GodotObject collider = Ray.GetCollider();
                if (collider is Projectile colliderProj && colliderProj.IgnoreOtherProjectiles)
                {
                    // return;
                }
                else if (collider is not (OobArea or DeflectorWall))
                {
                    RaiseCollision(this, CalculateRayCollisionData(delta, collider));
                    laserEnd = ToLocal(Ray.GetCollisionPoint());
                    Ray.TargetPosition = ToLocal(Ray.GetCollisionPoint());
                }
            }

            UpdateLaserEndPoint(laserEnd, colliding);
        }

        private void UpdateImpactParticles(Vector2 pos, float gRot)
        {
            _impactParticles.Position = pos;
            _impactParticles.GlobalRotation = gRot;
        }

        private void ActivateImpactParticles()
        {
            _impactParticlesActive = true;

            _impactParticles.Modulate = _baseLineMod;
            _impactParticles.Visible = true;
            _impactParticles.Restart();
        }

        private void DeactivateImpactParticles()
        {
            _impactParticlesActive = false;

            if (_impactTween != null && _impactTween.IsValid())
            {
                _impactTween.Kill();
            }

            // Create the transparent modulation color.
            Color trans = _baseLineMod;
            trans.A = 0;

            _impactTween = _impactParticles.CreateTween();

            _impactTween.TweenProperty(_impactParticles, "modulate", trans, 0.2f);
            _impactTween.TweenProperty(_impactParticles, "emitting", false, 0);
        }

        /// <summary>
        /// Updates the end point of the Line2D that is the visual representation of the laser.
        /// </summary>
        /// <param name="endPoint">The end point of the laser. This argument should be in local coordinates.</param>
        private void UpdateLaserEndPoint(Vector2 endPoint, bool colliding = false)
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

            // Impact stuff
            if (colliding)
            {
                if (!_impactParticlesActive)
                {
                    ActivateImpactParticles();
                }
                UpdateImpactParticles(endPoint, Ray.GetCollisionNormal().Angle());
            }
            else
            {
                if (_impactParticlesActive)
                {
                    DeactivateImpactParticles();
                }
            }
        }

        public void ReleaseTether()
        {
            _tethered = false;
            ToggleActive(false);
        }

        public void UpdateTether()
        {
            // Keep the laser fixed to the barrel
            if (_tBarrel != null && _active)
            {
                GlobalPosition = _tBarrel.GlobalPosition;
                GlobalRotation = _tBarrel.GlobalRotation;
            }
        }
    }
}
