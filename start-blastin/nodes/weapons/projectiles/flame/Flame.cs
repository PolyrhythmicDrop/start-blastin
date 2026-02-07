using System;
using Godot;
using Interfaces;
using Weapons;

namespace Projectiles
{
    [GlobalClass]
    public partial class Flame : Projectile, ITetheredProjectile
    {
        private GpuParticles2D _particles;
        private CollisionShape2D _collShape;

        // ~ ITetheredProjectile interface implementation ~
        private Barrel _tetheredBarrel;
        private bool _isTethered;

        public Barrel TetheredBarrel
        {
            get => _tetheredBarrel;
            set => _tetheredBarrel = value;
        }
        public bool IsTethered
        {
            get => _isTethered;
            set => _isTethered = value;
        }

        public override void _Ready()
        {
            base._Ready();

            _particles = GetNode<GpuParticles2D>("%FlameParticles");
            _collShape = GetNode<CollisionShape2D>("%CollisionShape2D");

            // Disable the raycast since we're not using it.
            _ray?.Enabled = false;
        }

        public override void SetProjectileCollisionLayers(Faction faction)
        {
            // Enable aura detection (Layer 6) for both types
            SetCollisionMaskValue(6, true);
            // // Enable shield collision detection
            // SetCollisionMaskValue(8, true);
            // // Enable OOB area detection
            // SetCollisionMaskValue(2, true);

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
                    // // Set the mask so the projectile hits player projectiles.
                    // SetCollisionMaskValue(4, true);
                    // Set the collision layer 5 (Projectiles-Enemy) to true.
                    SetCollisionLayerValue(5, true);
                    // Set the mask so the projectile does not hit other enemy projectiles.
                    SetCollisionMaskValue(5, false);
                    break;
                }
                case Faction.Players:
                {
                    // Set the mask so the projectile does not hit players.
                    SetCollisionMaskValue(1, false);
                    // Set the mask so that the projectile hits enemies.
                    SetCollisionMaskValue(3, true);
                    // Set the collision layer so that the projectile is a Player projectile.
                    SetCollisionLayerValue(4, true);
                    // Set the mask so the projectile does not hit player projectiles.
                    SetCollisionMaskValue(4, false);
                    // Set the collision layer 5 (Projectiles-Enemy) to false.
                    SetCollisionLayerValue(5, false);
                    // // Set the mask so the projectile hits enemy projectiles.
                    // SetCollisionMaskValue(5, true);
                    break;
                }
                case Faction.All:
                {
                    // Set all relevant masks and layers to true, except for the projectiles, since we don't want the flame to interact with other projectiles.
                    SetCollisionMaskValue(1, true);
                    SetCollisionMaskValue(3, true);
                    SetCollisionLayerValue(4, true);
                    // SetCollisionMaskValue(4, true);
                    SetCollisionLayerValue(5, true);
                    // SetCollisionMaskValue(5, true);
                    break;
                }
                case Faction.None:
                {
                    // Set all relevant masks and layers to false.
                    SetCollisionMaskValue(1, false);
                    SetCollisionMaskValue(3, false);
                    SetCollisionLayerValue(4, false);
                    SetCollisionMaskValue(4, false);
                    SetCollisionLayerValue(5, false);
                    SetCollisionMaskValue(5, false);
                    break;
                }
            }
        }

        public override void ToggleActive(bool active)
        {
            if (active)
            {
                // Normal projectile parenting
                _sourceWeapon.ProjectileParent.AddChild(this);
                _sourceWeapon.ActiveProjectileCount++;

                // Particles and collision
                _particles.Emitting = true;
                _collShape.Disabled = false;

                // Weapon will handle assigning a barrel and changing _isTethered, don't do it here.
            }
            else
            {
                // Normal projectile de-parenting
                _sourceWeapon.ProjectileParent.RemoveChild(this);
                _sourceWeapon.ActiveProjectileCount--;

                // Particles and collision
                _particles.Emitting = false;
                _collShape.Disabled = true;

                // Remove barrel assignment and tethering bool
                _isTethered = false;
                _tetheredBarrel = null;
            }

            _active = active;
            ToggleCollisionSignalConnection(active);

            // No need to toggle the deactivation timer for tethered projectiles.
        }

        /// <summary>
        /// Calls <see cref="UpdateTether"/> every frame if this Flame is active.
        /// </summary>
        /// <param name="delta">The time between frames.</param>
        public override void _PhysicsProcess(double delta)
        {
            if (_active)
            {
                UpdateTether();
            }
        }

        public void UpdateTether()
        {
            // Keep the flame fixed to the barrel
            if (_tetheredBarrel != null && _active)
            {
                GlobalPosition = _tetheredBarrel.GlobalPosition;
                GlobalRotation = _tetheredBarrel.GlobalRotation;
            }
        }

        public void ReleaseTether()
        {
            _isTethered = false;
            ToggleActive(false);
        }
    }
}
