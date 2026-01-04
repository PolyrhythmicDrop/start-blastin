using System;
using Godot;
using SafeResourcePicker;

namespace DataStructures
{
    [GlobalClass]
    public partial class PlayerSoundSet : SoundSet
    {
        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string PhaseStart { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string PhaseReady { get; set; }
    }
}
