using System.Collections.Generic;
using Godot;

namespace WaveManagement
{
    /// <summary>
    /// Adjusts the possible enemies that can spawn and the locations of spawners based on the current wave.
    /// </summary>
    [GlobalClass]
    public partial class SpawnerFormationScaler : WaveScaler
    {
        private List<SpawnerConfig> _formation;

        private Godot.Collections.Array<SpawnerConfig> _formationGD;

        public List<SpawnerConfig> Formation => _formation;

        [Export]
        public Godot.Collections.Array<SpawnerConfig> FormationGD
        {
            get => _formationGD;
            set
            {
                _formation = [.. value];
                _formationGD = value;
            }
        }
    }
}
