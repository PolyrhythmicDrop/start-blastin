using System;
using Godot;
using SafeResourcePicker;

namespace DataStructures
{
    /// <summary>
    /// A set of sounds played by an entity.
    /// </summary>
    [GlobalClass]
    public partial class SoundSet : Resource
    {
        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string Fire { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string Destruction { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string Spawn { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string MoveStart { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string Movement { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string MoveEnd { get; set; }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string Phase { get; set; }
    }
}
