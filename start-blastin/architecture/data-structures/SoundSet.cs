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
        public Node Parent;

        [Export]
        public AudioData Fire { get; set; }

        [Export]
        public AudioData Destruction { get; set; }

        [Export]
        public AudioData Spawn { get; set; }

        [Export]
        public AudioData MoveStart { get; set; }

        [Export]
        public AudioData Movement { get; set; }

        [Export]
        public AudioData MoveEnd { get; set; }

        [Export]
        public AudioData Hit { get; set; }

        [Export]
        public AudioData Block { get; set; }

        [Export]
        public AudioData Heal { get; set; }

        public void Initialize(Node parent)
        {
            Parent = parent;

            // Get all the properties using reflection
            foreach (var prop in GetType().GetProperties())
            {
                // Check if the property is an AudioData type and use pattern matching to cast the PropertyInfo type to AudioData.
                if (
                    prop.PropertyType == typeof(AudioData)
                    && prop.GetValue(this) is AudioData audioData
                )
                {
                    // Assign the source.
                    audioData.Source = parent;
                }
            }
        }
    }
}
