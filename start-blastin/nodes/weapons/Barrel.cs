using Godot;

namespace Weapons
{
    /// <summary>
    /// Firing point for projectiles.
    /// Barrels use rotation to determine their firing direction and have a position relative to their parent.
    /// </summary>
    [GlobalClass]
    public partial class Barrel : Node2D
    {
        /// <summary>
        /// Creates a new Barrel. The passed "rotation" value is used to set the firing direction.
        /// </summary>
        /// <param name="rotation">Rotation of the barrel, used to set firing direction.
        /// <list>
        /// <item>0 fires the same direction as the parent's rotation.</item>
        /// <item>Any other value is rotated relative to the parent's rotation.</item>
        /// </list>
        /// </param>
        public Barrel(float rotation)
        {
            Rotation = rotation;
        }

        public Barrel() { }
    }
}
