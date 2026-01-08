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
        private Area2D _despawnBlock;
        private CollisionShape2D _despawnShape;

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
            _despawnBlock = GetNode<Area2D>("%DespawnBlock");
            _despawnShape = _despawnBlock.GetNode<CollisionShape2D>("%DespawnShape2D");
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

        public void GenerateCelestialBody()
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

            CelestialBody body = CelestialBodyFactory.CreateCelestialBody();

            _bodyCanvas.AddChild(body);
            body.GlobalPosition = _spawnBlock.ToGlobal(position);
            DebugLogger.LogMessage($"GlobalPosition = {body.GlobalPosition}");
            _bodies.Add(body);

            if (body is IColorScheme schemed)
            {
                schemed.ApplyColorScheme(ColorScheme);
            }
        }
    }
}
