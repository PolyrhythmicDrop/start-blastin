using System;
using Godot;
using SafeResourcePicker;
using WaveManagement;

namespace Environmental
{
    public partial class DebugLevel : Node
    {
        [Export(SRP_HINT.RESOURCE_PATH, "SpawnerFormationScaler")]
        public string LevelOneOverride { get; set; }

        public override void _Ready()
        {
            WaveManager wm = GetNode<WaveManager>("WaveManager");
            if (LevelOneOverride != null)
            {
                wm.LevelOneScaler = LevelOneOverride;
            }
            wm.InitializeFirstWave();
        }
    }
}
