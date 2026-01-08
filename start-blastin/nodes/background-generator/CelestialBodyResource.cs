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
    }
}
