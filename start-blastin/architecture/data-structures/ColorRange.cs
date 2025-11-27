using Godot;

namespace DataStructures
{
    [GlobalClass]
    public partial class ColorRange : Resource
    {
        [Export]
        public Color Full { get; set; }

        [Export]
        public Color Mid { get; set; }

        [Export]
        public Color Low { get; set; }
    }
}
