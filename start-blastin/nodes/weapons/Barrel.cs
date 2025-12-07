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
        /// The direction the barrel is located on the ship. Generally corresponds to the rotation of the barrel, but not necessarily!
        /// </summary>
        public enum BarrelDirection
        {
            North,
            Northeast,
            East,
            Southeast,
            South,
            Southwest,
            West,
            Northwest,
        }

        private bool _active = false;

        private BarrelDirection _direction;
        public bool Active => _active;

        [Export]
        public BarrelDirection Direction
        {
            get => _direction;
            set => _direction = value;
        }

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

        public void ToggleActive(bool active)
        {
            _active = active;
        }

        public void RotateBarrel(float rads)
        {
            Rotate(rads);
        }
    }
}
