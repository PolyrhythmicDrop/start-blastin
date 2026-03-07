using System;
using Weapons;

namespace Interfaces
{
    /// <summary>
    /// Interface for tethered projectiles.
    /// Tethered projectiles are "rooted" to the barrel that fired them and stay active as long as fire is held (or under certain conditions, for enemies).
    /// </summary>
    public interface ITetheredProjectile
    {
        /// <summary>
        /// The barrel that this projectile is tethered to while active.
        /// </summary>
        Barrel TetheredBarrel { get; set; }

        /// <summary>
        /// Is this projectile currently tethered to a barrel?
        /// </summary>
        bool IsTethered { get; set; }

        /// <summary>
        /// Updates the tether's position and rotation based on the <see cref="TetheredBarrel"/> position and rotation.
        /// </summary>
        void UpdateTether();

        /// <summary>
        /// Releases the tether from the barrel.
        /// </summary>
        void ReleaseTether();
    }
}
