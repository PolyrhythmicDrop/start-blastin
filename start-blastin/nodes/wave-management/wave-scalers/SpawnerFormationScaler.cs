using System;
using Godot;

namespace WaveManagement
{
    public enum SpawnerLocation
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    [GlobalClass]
    public partial class SpawnerFormationScaler : WaveScaler
    {
        private Godot.Collections.Dictionary<SpawnerLocation, int> _formation;

        /// <summary>
        /// Layout and number of spawners at each location.
        /// </summary>
        /// <remarks>
        /// The Key is a <see cref="SpawnerLocation"/>, and the Value is the number of spawners that should be at that location.
        /// </remarks>
        [Export]
        public Godot.Collections.Dictionary<SpawnerLocation, int> Formation;
    }
}
