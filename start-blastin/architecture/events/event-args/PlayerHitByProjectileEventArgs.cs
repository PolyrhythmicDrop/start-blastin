using System;
using Projectiles;

namespace Events
{
    public class PlayerHitByProjectileEventArgs : EventArgs
    {
        public int PlayerId { get; }

        public Projectile Projectile { get; }

        public PlayerHitByProjectileEventArgs(int id, Projectile projectile)
        {
            PlayerId = id;
            Projectile = projectile;
        }
    }
}
