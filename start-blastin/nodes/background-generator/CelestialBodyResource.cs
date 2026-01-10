using System;
using Godot;

namespace BackgroundGenerator
{
    public enum CelestialBodyType
    {
        ShaderPlanet,
        BigStar,
    }

    [GlobalClass]
    public partial class CelestialBodyResource : Resource
    {
        [Export]
        public CelestialBodyType Type { get; set; }

        [Export]
        public Texture2D Texture { get; set; }

        [Export]
        public float ScaleFactor { get; set; }

        [Export]
        public float MinSpeed { get; set; }

        [Export]
        public float MaxSpeed { get; set; }

        [ExportGroup("Random Scaling")]
        [Export(PropertyHint.GroupEnable)]
        public bool RandomScale { get; set; }

        [Export(PropertyHint.Range, "0.05,10,greater_than")]
        public float MinScale { get; set; }

        [Export(PropertyHint.Range, "0.05,10,greater_than")]
        public float MaxScale { get; set; }
    }
}
