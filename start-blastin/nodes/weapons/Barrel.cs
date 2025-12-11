using System.Diagnostics;
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
        /// The direction the barrel fires in, relative to the direction of the ship. For example, North is straight forward.
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

        /// <summary>
        /// Is the barrel a base part of the entity?
        /// If you use the <see cref="Effects.ActivateBarrelEffect"/>, non-base barrels are freed when the effect is deactivated. Base barrels remain.
        /// </summary>
        [Export]
        public bool Base { get; set; }

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

        public Barrel(BarrelDirection direction)
        {
            _direction = Direction;
            // Change the rotation based on the barrel direction
            RotationDegrees = direction switch
            {
                BarrelDirection.North => 0,
                BarrelDirection.Northeast => 45,
                BarrelDirection.East => 90,
                BarrelDirection.Southeast => 135,
                BarrelDirection.South => 180,
                BarrelDirection.Southwest => 225,
                BarrelDirection.West => 270,
                BarrelDirection.Northwest => 315,
                _ => 0,
            };
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
