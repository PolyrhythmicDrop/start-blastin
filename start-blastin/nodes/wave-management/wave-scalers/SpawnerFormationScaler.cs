using System;
using Godot;

namespace WaveManagement
{
    public enum Location
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    [GlobalClass]
    public partial class SpawnerFormationScaler : WaveScaler
    {
        private Godot.Collections.Dictionary<Location, int> _formation;

        /// <summary>
        /// Layout and number of spawners at each location.
        /// </summary>
        /// <remarks>
        /// The Key is a <see cref="Location"/>, and the Value is the number of spawners that should be at that location.
        /// </remarks>
        [Export]
        public Godot.Collections.Dictionary<Location, int> Formation;
    }
}
