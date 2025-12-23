using Events;

namespace Interfaces
{
    /// <summary>
    /// Interface for entities and objects that can deflect projectiles.
    /// </summary>
    public interface IDeflect
    {
        bool DeflectEnabled { get; set; }
        void Deflect(IDeflect deflector);
    }
}
