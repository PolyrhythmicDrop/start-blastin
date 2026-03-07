using System;
using System.Threading.Tasks;
using Events;

namespace Interfaces
{
    public interface IDeflectable
    {
        bool IsBeingDeflected { get; }

        public Task Deflect(IDeflector deflector, CollisionEventArgs args = null);
    }
}
