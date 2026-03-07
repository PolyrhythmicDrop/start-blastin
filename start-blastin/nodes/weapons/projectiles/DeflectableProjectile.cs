using System;
using System.Threading.Tasks;
using Events;
using Godot;
using Interfaces;

namespace Projectiles
{
    public abstract partial class DeflectableProjectile : Projectile, IDeflectable
    {
        protected bool _isBeingDeflected = false;
        public bool IsBeingDeflected => _isBeingDeflected;

        /// <summary>
        /// Deflects the projectile based off the position and velocity of a <paramref name="deflector"/> object and a collision normal.
        /// Performs the deflection by adjusting the projectile's GlobalRotation, thus changing the direction and orientation of the projectile.
        /// </summary>
        /// <param name="deflector">The object the projectile has hit that is deflecting the projectile.</param>
        /// <param name="args">Collision arguments packaged with the <see cref="Collision"/> event. This method uses the <see cref="CollisionEventArgs.CollisionNormal"/> to calculate the deflection.
        /// If null, the deflected projectile rotates 180 degrees and continues on its way.</param>
        public async virtual Task Deflect(IDeflector deflector, CollisionEventArgs args = null)
        {
            if (_isBeingDeflected)
            {
                return;
            }

            _isBeingDeflected = true;

            // Convert to the opposite faction of the current faction.
            Faction newFaction = _faction == Faction.Players ? Faction.Enemies : Faction.Players;
            ConvertToNewFaction(newFaction);

            // Temporarily disable casting on this object
            _ray.Enabled = false;
            if (this is ShieldProjectile shield)
            {
                shield.ShapeCast.Enabled = false;
            }

            // Default naive deflection, 180deg from current rotation.
            if (args == null || args?.CollisionNormal == Vector2.Zero)
            {
                GlobalRotation += MathF.PI;
            }
            else
            {
                // Round the collision normal so it's easier to work with and more predictable.
                Vector2 roundColNormal = new(
                    MathF.Round(args.CollisionNormal.X, 1),
                    MathF.Round(args.CollisionNormal.Y, 1)
                );

                float normalRadians = roundColNormal.Angle();

                // Set the deflectee's rotation to opposite of the collision normal angle.
                GlobalRotation = normalRadians;

                if (deflector is IVelocityProvider velocitySource)
                {
                    // AddDeflectionVelocity(velocitySource.GetCurrentVelocity());
                    Vector2 deflectorVelocity = velocitySource.GetCurrentVelocity();
                    AddDeflectionVelocity(deflectorVelocity);
                }

                // Add the velocity of the deflector to the deflectee's speed.
            }

            if (this is not IDeflector thisDeflector)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                _isBeingDeflected = false;
                _ray.Enabled = true;
                return;
            }
            else if (thisDeflector.DeflectActive)
            {
                CollisionEventArgs newArgs = new(
                    this,
                    args.GlobalCollisionPoint,
                    args.CollisionNormal * -1
                );

                if (deflector is DeflectableProjectile proj && !proj.IsBeingDeflected)
                {
                    await proj.Deflect(thisDeflector, newArgs);
                }

                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                _isBeingDeflected = false;
                if (this is ShieldProjectile shield2)
                {
                    shield2.ShapeCast.Enabled = true;
                }
            }
        }
    }
}
