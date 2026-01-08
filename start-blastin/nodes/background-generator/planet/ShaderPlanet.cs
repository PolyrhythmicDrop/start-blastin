using System;
using Godot;
using Interfaces;
using Utility;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class ShaderPlanet : CelestialBody, IColorScheme
    {
        public override void _Ready()
        {
            Material = (ShaderMaterial)Material.Duplicate(true);
            double lightX = GD.RandRange(0.0, 1.0);
            double lightY = GD.RandRange(0.0, 1.0);
            Vector2 lightVect = new Vector2((float)lightX, (float)lightY);

            if (Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter("light_origin", lightVect);
                shaderMaterial.SetShaderParameter("seed", GD.RandRange(1.0, 10.0));
                shaderMaterial.SetShaderParameter("pixels", (int)(Scale.X * 100));
            }
        }

        public void ApplyColorScheme(GradientTexture1D scheme)
        {
            if (Material is ShaderMaterial planetMaterial)
            {
                planetMaterial.SetShaderParameter("colorscheme", scheme);
            }
        }

        public override void _Process(double delta)
        {
            MoveLocalY(SPEED * (float)delta, true);
        }
    }
}
