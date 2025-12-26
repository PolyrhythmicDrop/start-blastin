using Events;

namespace Interfaces
{
    /// <summary>
    /// Interface for entities and objects that can deflect projectiles.
    /// </summary>
    public interface IDeflector
    {
        bool DeflectActive { get; set; }
    }
}
