using System;
using System.Diagnostics;
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

        public static CelestialBody CreateCelestialBodyFromResource(CelestialBodyResource resource)
        {
            // Load the correct type of scene
            CelestialBodyType type = resource != null ? resource.Type : SelectRandomBodyType();

            CelestialBody body = type switch
            {
                CelestialBodyType.ShaderPlanet => CreateCelestialBody<ShaderPlanet>(
                    resource.RandomScale,
                    resource.ScaleFactor
                ),
                CelestialBodyType.BigStar => CreateCelestialBody<BigStar>(
                    resource.RandomScale,
                    resource.ScaleFactor
                ),
                _ => CreateCelestialBody<ShaderPlanet>(resource.RandomScale, resource.ScaleFactor),
            };

            body.SetSpeed = (float)GD.RandRange(resource.MinSpeed, resource.MaxSpeed);

            return body;
        }

        public static T CreateCelestialBody<T>(bool randomScale = false, float? scaling = null)
            where T : CelestialBody
        {
            CelestialBody body = typeof(T) switch
            {
                Type t when t == typeof(ShaderPlanet) =>
                    _shaderPlanetScene.Instantiate<ShaderPlanet>(),
                Type t when t == typeof(BigStar) => _bigStarScene.Instantiate<BigStar>(),
                _ => _shaderPlanetScene.Instantiate<ShaderPlanet>(),
            };

            if (randomScale)
            {
                float scaleFactor = MathF.Round(
                    (float)GD.RandRange(body.MinScale, body.MaxScale),
                    3
                );
                Vector2 planetScale = Vector2.One * scaleFactor;
                body.Scale = planetScale;
            }
            else
            {
                body.Scale =
                    Vector2.One * (scaling ?? (float)GD.RandRange(body.MinScale, body.MaxScale));
            }

            body.Name = $"{typeof(T).Name}-{Nanoid.Generate(size: 3)}";

            return (T)body;
        }

        public static CelestialBody CreateRandomCelestialBody()
        {
            CelestialBodyType type = SelectRandomBodyType();

            return type switch
            {
                CelestialBodyType.ShaderPlanet => CreateCelestialBody<ShaderPlanet>(
                    randomScale: true
                ),
                CelestialBodyType.BigStar => CreateCelestialBody<BigStar>(randomScale: true),
                _ => CreateCelestialBody<BigStar>(randomScale: true),
            };
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
