using Godot;
using Utility;

namespace Enemies.Spawners
{
    [GlobalClass]
    // [Tool]
    public partial class SquadronLayout : Resource
    {
        private Godot.Collections.Array<Vector2> _offsets = new() { Vector2.Zero };

        [Export]
        public Godot.Collections.Array<Vector2> Offsets
        {
            get => _offsets;
            set => _offsets = value;
        }
    }
}
