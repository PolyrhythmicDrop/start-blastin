using System;
using System.Collections.Generic;
using Factories;
using Godot;
using Weapons;

namespace Projectiles
{
    public class ProjectilePool : List<Projectile>
    {
        private WeaponNode _weapon;

        public ProjectilePool(WeaponNode weapon, int initCapacity)
        {
            _weapon = weapon;
            Capacity = initCapacity;
            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < Capacity; i++)
            {
                Add(CreateProjectile());
            }
        }

        public new void Add(Projectile proj)
        {
            EnsureCapacity(Count + 1);
            base.Add(proj);
        }

        public Projectile CreateProjectile()
        {
            if (_weapon == null)
            {
                GD.PrintErr("ProjectilePool: _weapon is null in CreateProjectile!");
                throw new InvalidOperationException(
                    "ProjectilePool: _weapon is null in CreateProjectile!"
                );
            }
            if (_weapon.Stats == null)
            {
                GD.PrintErr(
                    $"ProjectilePool: _weapon.Stats is null in CreateProjectile for {_weapon.Name}!"
                );
                throw new InvalidOperationException(
                    "ProjectilePool: _weapon.Stats is null in CreateProjectile!"
                );
            }
            Projectile proj = ProjectileFactory.CreateProjectile(_weapon.Stats.ProjType);
            // Assign the Z-index so it always appears behind the weapon
            // proj.ZIndex = _weapon.ZIndex - 1;

            // Give the projectile a unique Godot-valid name
            proj.Name = StringExtensions.ValidateNodeName(
                proj.GetType().Name + (int)DateTime.Now.Ticks
            );

            return proj;
        }

        public void ActivateProjectile(Projectile proj)
        {
            try
            {
                // Activate projectile if it's in the pool.
                if (!Contains(proj))
                {
                    throw new InvalidOperationException(
                        proj.Name + " does not exist in the projectile pool!"
                    );
                }

                if (!_weapon.ProjectileParent.IsAncestorOf(proj) && !proj.Active)
                {
                    proj.Active = true;
                    _weapon.ProjectileParent.CallDeferred(Node.MethodName.AddChild, proj);
                    proj.DeactivationTimer.Timeout += () => DeactivateProjectile(proj);
                }
                else
                {
                    throw new InvalidOperationException(
                        proj.Name + " either has a parent already or is already active!"
                    );
                }

                // Connect signals
                if (!proj.IsConnected(Projectile.SignalName.Collision, _weapon.HitCallable))
                {
                    proj.Connect(Projectile.SignalName.Collision, _weapon.HitCallable, 4);
                }
                else
                {
                    throw new InvalidOperationException(
                        proj.Name + " is already connected to " + Projectile.SignalName.Collision
                    );
                }
            }
            catch (InvalidOperationException e)
            {
                GD.PushError(e);
            }
        }

        public void DeactivateProjectile(Projectile proj)
        {
            try
            {
                if (!Contains(proj))
                {
                    throw new InvalidOperationException(
                        proj.Name + " does not exist in the projectile pool!"
                    );
                }

                if (_weapon.ProjectileParent.IsAncestorOf(proj) && proj.Active)
                {
                    proj.DeactivationTimer.Stop();
                    proj.Active = false;
                    _weapon.ProjectileParent.CallDeferred(Node.MethodName.RemoveChild, proj);
                }
                else
                {
                    throw new InvalidOperationException(
                        proj.Name + " is not active or is not part of the scene tree!"
                    );
                }

                if (proj.IsConnected(Projectile.SignalName.Collision, _weapon.HitCallable))
                {
                    proj.Disconnect(Projectile.SignalName.Collision, _weapon.HitCallable);
                }
            }
            catch (InvalidOperationException e)
            {
                GD.PushError(e);
            }
        }

        public Projectile RequestProjectile()
        {
            // Search for any inactive projectiles already existing in the pool.
            foreach (Projectile proj in this)
            {
                if (!proj.Active && !proj.IsAncestorOf(_weapon))
                {
                    // Enable processing by re-adding the projectile as a child and activating it.
                    ActivateProjectile(proj);
                    return proj;
                }
            }

            // If no inactive projectiles are found
            // Create a new projectile, activate it, add it to the pool, and return it.
            Projectile ammo = CreateProjectile();
            Add(ammo);
            ActivateProjectile(ammo);

            return ammo;
        }

        public void CullPool()
        {
            Predicate<Projectile> Inactive = (Projectile proj) =>
            {
                return proj.Active == false ? true : false;
            };
            foreach (Projectile inactive in FindAll(Inactive))
            {
                if (!inactive.IsQueuedForDeletion())
                {
                    inactive.QueueFree();
                }
            }
            RemoveAll(Inactive);
            TrimExcess();
        }
    }
}
