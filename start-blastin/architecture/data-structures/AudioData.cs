using System;
using Godot;
using NanoidDotNet;
using SafeResourcePicker;

namespace DataStructures
{
    /// <summary>
    /// Data packet for audio data. Pass to the <see cref="AudioService"/> to play a sound using the parameters.
    /// </summary>
    [GlobalClass]
    public partial class AudioData : Resource
    {
        public Node Source { get; set; } = null;

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamWAV")]
        public string Sound { get; set; }

        public readonly string AudioID = Nanoid.Generate(size: 8);

        [Export]
        public float Volume { get; set; }

        [Export(PropertyHint.Range, "1,20,or_greater")]
        public int MaxPolyphony { get; set; } = 5;

        [ExportGroup("Positioning")]
        [Export(PropertyHint.GroupEnable)]
        public bool Positional { get; set; } = false;

        [Export]
        public float Attenuation { get; set; } = 1;

        [Export]
        public float MaxDistance { get; set; } = 2000;

        [ExportGroup("Randomization")]
        [Export(PropertyHint.GroupEnable)]
        public bool Randomized { get; set; }

        [Export]
        public float RandomPitch { get; set; } = 1.0f;

        [Export]
        public float RandomVolume { get; set; } = 0.0f;

        [ExportGroup("Loop")]
        [Export(PropertyHint.GroupEnable)]
        public bool Looping { get; set; } = false;

        [Export]
        public AudioStreamWav.LoopModeEnum LoopMode { get; set; } =
            AudioStreamWav.LoopModeEnum.Disabled;

        [Export]
        public int LoopBegin { get; set; } = 0;

        [Export]
        public int LoopEnd { get; set; } = 0;

        public AudioStream GenerateAudioStream()
        {
            AudioStreamWav stream = ResourceLoader.Load<AudioStreamWav>(Sound);

            // Set looping parameters.
            if (Looping)
            {
                stream.LoopMode = LoopMode;
                stream.LoopBegin = LoopBegin;
                stream.LoopEnd = LoopEnd;
            }
            else
            {
                stream.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
            }

            // Convert the UID to a path, trim the .tres suffix, split according to the '/' character, and get the penultimate entry in the new array.
            string pathStr = ResourceUid.UidToPath(Sound).TrimSuffix(".tres").Split('/')[^1];
            stream.ResourceName = pathStr;

            if (Randomized)
            {
                AudioStreamRandomizer randomizer = new();
                randomizer.AddStream(-1, stream);
                randomizer.RandomPitch = RandomPitch;
                randomizer.RandomVolumeOffsetDb = RandomVolume;
                return randomizer;
            }
            else
            {
                return stream;
            }
        }
    }
}
