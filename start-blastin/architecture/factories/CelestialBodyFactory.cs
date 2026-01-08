using System;
using Autoloads;
using BackgroundGenerator;
using Godot;

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

        public static CelestialBody CreateCelestialBody(CelestialBodyResource resource = null)
        {
            // Load the correct type of scene
            CelestialBodyType type = resource != null ? resource.Type : SelectRandomBodyType();

            CelestialBody body = type switch
            {
                CelestialBodyType.ShaderPlanet => _shaderPlanetScene.Instantiate<ShaderPlanet>(),
                CelestialBodyType.BigStar => _bigStarScene.Instantiate<BigStar>(),
                _ => _shaderPlanetScene.Instantiate<ShaderPlanet>(),
            };

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
