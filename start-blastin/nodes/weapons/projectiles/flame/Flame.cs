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
        }

        public override void ToggleActive(bool active)
        {
            if (active)
            {
                // Normal projectile parenting
                _sourceWeapon.ProjectileParent.AddChild(this);
                _sourceWeapon.ActiveProjectileCount++;

                // Partilces and collision
                _particles.Emitting = true;
                _collShape.Disabled = false;

                // Weapon will handle assigning a barrel and changing _isTethered, don't do it here.
            }
            else
            {
                // Normal projectile de-parenting
                _sourceWeapon.ProjectileParent.RemoveChild(this);
                _sourceWeapon.ActiveProjectileCount--;

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
