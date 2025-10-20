using System;
using Godot;

namespace Interfaces
{
    /// <summary>
    /// Provides a Vector2 velocity from any object.
    /// </summary>
    public interface IVelocityProvider
    {
        /// <summary>
        /// Returns the current velocity of the object.
        /// </summary>
        Vector2 GetCurrentVelocity();
    }
}
