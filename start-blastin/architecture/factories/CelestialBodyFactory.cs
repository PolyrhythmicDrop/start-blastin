using System;
using Autoloads;
using BackgroundGenerator;
using Godot;
using NanoidDotNet;

namespace Factories
{
    public static class CelestialBodyFactory
    {
        private static PackedScene _shaderPlanetScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/background-generator/planet/shader-planet.tscn"
        );
        private static PackedScene _bigStarScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/background-generator/big-star/big-star.tscn"
        );

        public static CelestialBody CreateCelestialBody(
            CelestialBodyResource resource = null,
            bool randomScale = false
        )
        {
            // Load the correct type of scene
            CelestialBodyType type = resource != null ? resource.Type : SelectRandomBodyType();

            CelestialBody body = type switch
            {
                CelestialBodyType.ShaderPlanet => _shaderPlanetScene.Instantiate<ShaderPlanet>(),
                CelestialBodyType.BigStar => _bigStarScene.Instantiate<BigStar>(),
                _ => _shaderPlanetScene.Instantiate<ShaderPlanet>(),
            };

            if (randomScale)
            {
                float scaleFactor = (float)GD.RandRange(0.2, 0.9) * (float)GD.RandRange(0.1, 10);
                Vector2 planetScale = Vector2.One * scaleFactor;
                body.Scale = planetScale;
            }

            body.Name = $"{body.GetType().Name}-{Nanoid.Generate(size: 3)}";

            return body;
        }

        /// <summary>
        /// Selects and returns a random celestial body type to create. Expand this when there are more types.
        /// </summary>
        /// <returns></returns>
        public static CelestialBodyType SelectRandomBodyType()
        {
            int selection = GD.RandRange(1, 10);

            if (selection % 2 == 0)
            {
                return CelestialBodyType.ShaderPlanet;
            }
            else
            {
                return CelestialBodyType.BigStar;
            }
        }
    }
}
