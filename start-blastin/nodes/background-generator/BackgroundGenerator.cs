using System;
using System.Collections.Generic;
using Godot;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class BackgroundGenerator : Control
    {
        private ColorRect _background;
        private ColorRect _starStuff;
        private ColorRect _nebulae;
        private GpuParticles2D _particles;
        private Node2D _starContainer;
        private Node2D _planetContainer;
        private Timer _pauseParticleTimer;

        private PackedScene _planetScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/background-generator/planet/planet.tscn"
        );
        private PackedScene _bigStarScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/background-generator/big-star/big-star.tscn"
        );

        private bool _shouldTile = false;
        private bool _reduceBackground = false;
        private Vector2 _mirrorSize = new Vector2(200, 200);

        private List<Planet> _planetObjects = new();
        private List<BigStar> _starObjects = new();

        [Export]
        public GradientTexture1D ColorScheme { get; set; }

        public override void _Ready()
        {
            _background = GetNode<ColorRect>("%Background");
            _starStuff = GetNode<ColorRect>("%StarStuff");
            _nebulae = GetNode<ColorRect>("%Nebulae");
            _particles = GetNode<GpuParticles2D>("%StarParticles");
            _starContainer = GetNode<Node2D>("%StarContainer");
            _planetContainer = GetNode<Node2D>("%PlanetContainer");
            _pauseParticleTimer = GetNode<Timer>("%PauseParticles");

            _pauseParticleTimer.Timeout += OnPauseParticlesTimeout;

            SetNewColors(ColorScheme, _background.Color);

            GenerateNewBackground();
        }

        public void SetMirrorSize(Vector2 size)
        {
            _mirrorSize = size;
        }

        public void ToggleTile()
        {
            // Set shouldTile to the opposite
            _shouldTile = !_shouldTile;

            if (_starStuff.Material is ShaderMaterial starStuffMat)
            {
                starStuffMat.SetShaderParameter("should_tile", _shouldTile);
            }

            if (_nebulae.Material is ShaderMaterial nebulaeMat)
            {
                nebulaeMat.SetShaderParameter("should_tile", _shouldTile);
            }

            MakeNewPlanets();
            MakeNewStars();
        }

        public void ToggleReduceBackground()
        {
            _reduceBackground = !_reduceBackground;

            if (_starStuff.Material is ShaderMaterial starStuffMat)
            {
                starStuffMat.SetShaderParameter("reduce_background", _reduceBackground);
            }

            if (_nebulae.Material is ShaderMaterial nebulaeMat)
            {
                nebulaeMat.SetShaderParameter("reduce_background", _reduceBackground);
            }
        }

        public void GenerateNewBackground()
        {
            if (
                _starStuff.Material is not ShaderMaterial starStuffMaterial
                || _nebulae.Material is not ShaderMaterial nebulaeMaterial
            )
            {
                return;
            }

            Vector2 rectSize = GetRect().Size;

            // Set StarStuff seed and pixels
            starStuffMaterial.SetShaderParameter("seed", GD.RandRange(1, 10));
            starStuffMaterial.SetShaderParameter("pixels", MathF.Max(rectSize.X, rectSize.Y));

            // Set the aspect ratio
            Vector2 aspect;
            if (rectSize.X > rectSize.Y)
            {
                aspect = new Vector2(rectSize.X / rectSize.Y, 1.0f);
            }
            else
            {
                aspect = new Vector2(1.0f, rectSize.Y / rectSize.X);
            }

            // Set shader parameters using the aspect ratio
            starStuffMaterial.SetShaderParameter("uv_correct", aspect);
            nebulaeMaterial.SetShaderParameter("uv_correct", aspect);
            nebulaeMaterial.SetShaderParameter("seed", GD.RandRange(1.0, 10.0));
            nebulaeMaterial.SetShaderParameter("pixels", MathF.Max(rectSize.X, rectSize.Y));

            // Set properties for the particles.
            _particles.SpeedScale = 1.0;
            _particles.Amount = 1;
            _particles.Position = rectSize * 0.5f;
            if (_particles.ProcessMaterial is ShaderMaterial particleMaterial)
            {
                particleMaterial.SetShaderParameter(
                    "emission_box_extents",
                    new Vector3(rectSize.X * 0.5f, rectSize.Y * 0.5f, 1.0f)
                );
            }

            float particleAmount = (rectSize.X * rectSize.Y) / 150;
            int x = (int)(particleAmount * 0.75f);
            int y = (int)(particleAmount * 0.25f);
            _particles.Amount = Math.Max(1, (int)GD.Randi() % (x + y));

            _pauseParticleTimer.Start();

            MakeNewPlanets();
            MakeNewStars();
        }

        public void MakeNewStars()
        {
            foreach (BigStar s in _starObjects)
            {
                s.QueueFree();
            }

            _starObjects.Clear();

            Vector2 rectSize = GetRect().Size;

            int starAmount = (int)(MathF.Max(rectSize.X, rectSize.Y) / 20);
            starAmount = Math.Max(starAmount, 1);

            for (int i = 0; i < GD.Randi() % starAmount; i++)
            {
                PlaceBigStar();
            }
        }

        public void MakeNewPlanets()
        {
            foreach (Planet p in _planetObjects)
            {
                p.QueueFree();
            }
            _planetObjects.Clear();

            // Hard-coded for now, maybe make this randomized or settable in future iterations.
            int planetAmt = 5;

            for (int i = 0; i < GD.Randi() % planetAmt; i++)
            {
                PlacePlanet();
            }
        }

        public void PlaceBigStar()
        {
            Vector2 pos;
            Vector2 rectSize = GetRect().Size;

            if (_shouldTile)
            {
                float offset = 10.0f;
                pos = new Vector2(
                    (int)GD.RandRange(offset, rectSize.X - offset),
                    (int)GD.RandRange(offset, rectSize.Y - offset)
                );
            }
            else
            {
                pos = new Vector2(
                    (int)GD.RandRange(0, rectSize.X),
                    (int)GD.RandRange(0, rectSize.Y)
                );
            }

            BigStar star = _bigStarScene.Instantiate<BigStar>();

            if (star.Material is ShaderMaterial starMaterial)
            {
                starMaterial.SetShaderParameter("colorscheme", ColorScheme);
            }

            star.Position = pos;
            _starContainer.AddChild(star);
            _starObjects.Add(star);
        }

        public void PlacePlanet()
        {
            Vector2 rectSize = GetRect().Size;

            // Set the planet's scale
            float minSize = Math.Min(rectSize.X, rectSize.Y);
            float scaleFactor =
                (float)GD.RandRange(0.2, 0.7) * (float)GD.RandRange(0.5, 1.0) * minSize * 0.005f;
            Vector2 planetScale = Vector2.One * scaleFactor;

            // Set the planet's position.
            int xPos;
            int yPos;
            if (_shouldTile)
            {
                float offset = Scale.X * 100 * 0.5f;
                xPos = (int)GD.RandRange(offset, rectSize.X - offset);
                yPos = (int)GD.RandRange(offset, rectSize.Y - offset);
            }
            else
            {
                xPos = (int)GD.RandRange(0, rectSize.X);
                yPos = (int)GD.RandRange(0, rectSize.Y);
            }
            Vector2 pos = new Vector2(xPos, yPos);

            Planet planet = _planetScene.Instantiate<Planet>();

            if (planet.Material is ShaderMaterial planetMaterial)
            {
                planetMaterial.SetShaderParameter("colorscheme", ColorScheme);
            }

            planet.Scale = planetScale;
            planet.Position = pos;
            _planetContainer.AddChild(planet);
            _planetObjects.Add(planet);
        }

        public void OnPauseParticlesTimeout()
        {
            // _particles.SpeedScale = 0.0f;
        }

        public void SetBackgroundColor(Color color)
        {
            _background.Color = color;
            if (_nebulae.Material is ShaderMaterial nebulaeMaterial)
            {
                nebulaeMaterial.SetShaderParameter("background_color", color);
            }
        }

        public void SetNewColors(GradientTexture1D scheme, Color bgColor)
        {
            ColorScheme = scheme;

            if (
                _starStuff.Material is not ShaderMaterial starStuffMaterial
                || _nebulae.Material is not ShaderMaterial nebulaeMaterial
                || _particles.ProcessMaterial is not ShaderMaterial particleMaterial
            )
            {
                return;
            }

            starStuffMaterial.SetShaderParameter("colorscheme", ColorScheme);
            nebulaeMaterial.SetShaderParameter("colorscheme", ColorScheme);
            nebulaeMaterial.SetShaderParameter("background_color", bgColor);

            particleMaterial.SetShaderParameter("colorscheme", ColorScheme);

            foreach (Planet p in _planetObjects)
            {
                if (p.Material is ShaderMaterial pMat)
                {
                    pMat.SetShaderParameter("colorscheme", ColorScheme);
                }
            }

            foreach (BigStar s in _starObjects)
            {
                if (s.Material is ShaderMaterial sMat)
                {
                    sMat.SetShaderParameter("colorscheme", ColorScheme);
                }
            }
        }

        public void ToggleDust()
        {
            _starStuff.Visible = !_starStuff.Visible;
        }

        public void ToggleStars()
        {
            _starContainer.Visible = !_starContainer.Visible;
            _particles.Visible = !_particles.Visible;
        }

        public void ToggleNebulae()
        {
            _nebulae.Visible = !_nebulae.Visible;
        }

        public void TogglePlanets()
        {
            _planetContainer.Visible = !_planetContainer.Visible;
        }

        public void ToggleTransparency()
        {
            _background.Visible = !_background.Visible;
        }
    }
}
