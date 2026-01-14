using System;
using Godot;
using SafeResourcePicker;

namespace DataStructures
{
    [GlobalClass]
    public partial class PlayerSoundSet : SoundSet
    {
        [Export]
        public AudioData PhaseStart { get; set; }

        [Export]
        public AudioData PhaseReady { get; set; }
    }
}
