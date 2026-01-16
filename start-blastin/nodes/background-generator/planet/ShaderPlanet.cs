using System;
using Godot;
using Interfaces;
using Utility;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class ShaderPlanet : CelestialBody, IColorScheme
    {
        // Set defaults in constructor. These can be overridden later, either in the factory or with a resource.
        public ShaderPlanet()
        {
            _minScale = 0.1f;
            _maxScale = 6.0f;

            _minSpeed = 1.0f;
            _maxSpeed = 35.0f;
        }

        public override void _Ready()
        {
            if (SetSpeed <= 0)
            {
                SetSpeed = (float)GD.RandRange(_minSpeed, _maxSpeed);
            }
            _sprite = GetNode<Sprite2D>("%ShaderPlanetSprite");
            base._Ready();

            _sprite.Material = (ShaderMaterial)_sprite.Material.Duplicate(true);

            double lightX = GD.RandRange(0.0, 1.0);
            double lightY = GD.RandRange(0.0, 1.0);
            Vector2 lightVect = new Vector2((float)lightX, (float)lightY);

            if (_sprite.Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter("light_origin", lightVect);
                shaderMaterial.SetShaderParameter("seed", GD.RandRange(1.0, 10.0));
                shaderMaterial.SetShaderParameter("pixels", (int)(Scale.X * 100));
            }

            DebugLogger.LogMessage($"{Name} set speed: {SetSpeed}");
        }

        public void ApplyColorScheme(GradientTexture1D scheme)
        {
            if (_sprite.Material is ShaderMaterial planetMaterial)
            {
                planetMaterial.SetShaderParameter("colorscheme", scheme);
            }
        }
    }
}
