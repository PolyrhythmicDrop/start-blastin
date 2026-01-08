using System;
using System.Collections.Generic;
using Autoloads;
using Factories;
using Godot;
using Interfaces;
using Utility;

namespace BackgroundGenerator
{
    [GlobalClass]
    public partial class ScrollingBackground : Node, IListener
    {
        private ColorRect _background;
        private Area2D _spawnBlock;
        private CollisionShape2D _spawnShape;

        private GpuParticles2D _particles;
        private Parallax2D _parallax;

        private int _maxBodies = 3;
        private CanvasLayer _bodyCanvas;

        private List<CelestialBody> _bodies = new();

        [Export]
        public int MaxBodies
        {
            get => _maxBodies;
            set => _maxBodies = value;
        }

        [Export]
        public GradientTexture1D ColorScheme { get; set; }

        public override void _Ready()
        {
            _background = GetNode<ColorRect>("%Background");
            _spawnBlock = GetNode<Area2D>("%SpawnBlock");
            _spawnShape = _spawnBlock.GetNode<CollisionShape2D>("%SpawnShape2D");

            _particles = GetNode<GpuParticles2D>("%StarParticles");
            _parallax = GetNode<Parallax2D>("%Parallax2D");

            GenerateStarParticles();

            _bodyCanvas = GetNode<CanvasLayer>("%CelestialBody-CanvasLayer");

            ConnectSignals();
        }

        public void ConnectSignals() { }

        public void DisconnectSignals() { }

        public override void _Process(double delta)
        {
            if (_bodies.Count < MaxBodies)
            {
                GenerateCelestialBody();
            }
        }

        private void GenerateCelestialBody()
        {
            // Select a position for the body from somewhere within the spawning area
            Rect2 spawnRect = _spawnShape.Shape.GetRect();
            int x = GD.RandRange(
                (int)spawnRect.Position.X,
                (int)(spawnRect.Position.X + spawnRect.Size.X)
            );
            int y = GD.RandRange(
                (int)spawnRect.Position.Y,
                (int)(spawnRect.Position.Y + spawnRect.Size.Y)
            );

            Vector2 position = new Vector2(x, y);

            CelestialBody body = CelestialBodyFactory.CreateCelestialBody(randomScale: true);

            _bodyCanvas.AddChild(body);
            body.GlobalPosition = _spawnBlock.ToGlobal(position);
            ConnectCelestialBodySignals(body);
            _bodies.Add(body);

            if (body is IColorScheme schemed)
            {
                schemed.ApplyColorScheme(ColorScheme);
            }
        }

        private void ConnectCelestialBodySignals(CelestialBody body)
        {
            body.VisibleNotifier.ScreenExited += () => RemoveCelestialBody(body);
        }

        private void RemoveCelestialBody(CelestialBody body)
        {
            DebugLogger.LogMessage($"Removing {body.Name}!");
            _bodies.Remove(body);
            if (IsInstanceValid(body) && !body.IsQueuedForDeletion())
            {
                body.QueueFree();
            }
        }

        private void GenerateStarParticles()
        {
            // _particles.SpeedScale = 1.0;
            // _particles.Amount = 1;

            var parallaxSize = _parallax.RepeatSize;
            if (_particles.ProcessMaterial is ShaderMaterial particleMaterial)
            {
                particleMaterial.SetShaderParameter(
                    "emission_box_extents",
                    new Vector3(parallaxSize.X, parallaxSize.Y, 1.0f)
                );
                particleMaterial.SetShaderParameter("colorscheme", ColorScheme);
            }

            // float particleAmount = (parallaxSize.X * parallaxSize.Y) / _particles.Amount;
            // int x = (int)(particleAmount * 0.75f);
            // int y = (int)(particleAmount * 0.25f);
            // _particles.Amount = Math.Max(20, (int)GD.Randi() % (x + y));
        }
    }
}
