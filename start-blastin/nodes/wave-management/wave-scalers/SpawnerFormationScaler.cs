using System.Collections.Generic;
using Godot;

namespace WaveManagement
{
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
