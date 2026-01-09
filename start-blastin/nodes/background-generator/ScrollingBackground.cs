using System;
using System.Collections.Generic;
using Autoloads;
using Events;
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

        private int _maxPlanets = 3;
        private int _maxBigStars = 5;
        private CanvasLayer _bodyCanvas;

        private List<CelestialBody> _bodies = new();

        private List<ShaderPlanet> _shaderPlanets = new();
        private List<BigStar> _bigStars = new();

        // ~~ Spawn timers ~~
        private Timer _planetSpawnTimer;
        private Timer _bigStarSpawnTimer;

        [Export]
        public int MaxPlanets
        {
            get => _maxPlanets;
            set => _maxPlanets = value;
        }

        [Export]
        public int MaxBigStars
        {
            get => _maxBigStars;
            set => _maxBigStars = value;
        }

        [Export]
        public GradientTexture1D ColorScheme { get; set; }

        public override void _Ready()
        {
            _background = GetNode<ColorRect>("%Background");
            _spawnBlock = GetNode<Area2D>("%SpawnBlock");
            _spawnShape = _spawnBlock.GetNode<CollisionShape2D>("%SpawnShape2D");

            _planetSpawnTimer = GetNode<Timer>("%ShaderPlanetSpawnTimer");
            _bigStarSpawnTimer = GetNode<Timer>("%BigStarSpawnTimer");

            RandomizeSpawnTime(_planetSpawnTimer);
            RandomizeSpawnTime(_bigStarSpawnTimer);

            _particles = GetNode<GpuParticles2D>("%StarParticles");
            _parallax = GetNode<Parallax2D>("%Parallax2D");

            _bodyCanvas = GetNode<CanvasLayer>("%CelestialBody-CanvasLayer");

            GenerateStarParticles();

            ConnectSignals();

            _planetSpawnTimer.Start();
            _bigStarSpawnTimer.Start();
        }

        public void ConnectSignals()
        {
            // Connect the body spawn timers.
            _planetSpawnTimer.Timeout += () => TrySpawnBody(CelestialBodyType.ShaderPlanet);
            _bigStarSpawnTimer.Timeout += () => TrySpawnBody(CelestialBodyType.BigStar);

            // Connect wave timer for wave-based variations.
            EventBus.Instance.WaveStarted += OnWaveStarted;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.WaveStarted -= OnWaveStarted;
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }

        private void RandomizeSpawnTime(Timer timer)
        {
            float factor = (float)GD.RandRange(0.2, 3);
            timer.WaitTime *= factor;
        }

        private void OnWaveStarted(object source, WaveStartedEventArgs args)
        {
            // Change the starfield every 3 waves.
            bool changeStarfield = args.Wave % 3 == 0;

            if (changeStarfield)
            {
                GenerateStarParticles();
            }
        }

        public override void _Process(double delta) { }

        /// <summary>
        /// Rolls to see if we should spawn a new celestial body.
        /// </summary>
        /// <param name="type"></param>
        public void TrySpawnBody(CelestialBodyType type)
        {
            switch (type)
            {
                case CelestialBodyType.ShaderPlanet:
                {
                    if (_shaderPlanets.Count < MaxPlanets)
                    {
                        bool generate = GD.Randi() % 2 == 0;
                        DebugLogger.LogMessage(
                            $"Let's see if we should spawn a ShaderPlanet: {generate}"
                        );
                        if (generate)
                        {
                            GenerateCelestialBody(type);
                        }
                        RandomizeSpawnTime(_planetSpawnTimer);
                        _planetSpawnTimer.Start();
                    }
                    break;
                }
                case CelestialBodyType.BigStar:
                    if (_bigStars.Count < MaxBigStars)
                    {
                        bool generate = GD.Randi() % 2 == 0;
                        DebugLogger.LogMessage(
                            $"Let's see if we should spawn a BigStar: {generate}"
                        );
                        if (generate)
                        {
                            GenerateCelestialBody(type);
                        }
                        RandomizeSpawnTime(_bigStarSpawnTimer);
                        _bigStarSpawnTimer.Start();
                    }
                    break;
            }
        }

        private void GenerateCelestialBody(CelestialBodyType? type = null)
        {
            DebugLogger.LogMessage($"Generating a celestial body of type {type}!");

            CelestialBody body = type switch
            {
                CelestialBodyType.ShaderPlanet => GenerateShaderPlanet(),
                CelestialBodyType.BigStar => GenerateBigStar(),
                _ => CelestialBodyFactory.CreateRandomCelestialBody(),
            };

            _bodyCanvas.AddChild(body);

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
            body.GlobalPosition = _spawnBlock.ToGlobal(position);

            ConnectCelestialBodySignals(body);

            if (body is IColorScheme schemed)
            {
                schemed.ApplyColorScheme(ColorScheme);
            }
        }

        private ShaderPlanet GenerateShaderPlanet()
        {
            ShaderPlanet planet = CelestialBodyFactory.CreateCelestialBody<ShaderPlanet>(
                randomScale: true
            );

            _shaderPlanets.Add(planet);

            return planet;
        }

        private BigStar GenerateBigStar()
        {
            BigStar bigStar = CelestialBodyFactory.CreateCelestialBody<BigStar>(randomScale: true);

            _bigStars.Add(bigStar);

            return bigStar;
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

            _particles.Emitting = false;

            var parallaxSize = _parallax.RepeatSize;
            if (_particles.ProcessMaterial is ShaderMaterial particleMaterial)
            {
                particleMaterial.SetShaderParameter(
                    "emission_box_extents",
                    new Vector3(parallaxSize.X, parallaxSize.Y, 1.0f)
                );
                particleMaterial.SetShaderParameter("colorscheme", ColorScheme);
            }

            float particleAmount = (parallaxSize.X * parallaxSize.Y) / _particles.Amount;
            int x = (int)(particleAmount * 0.75f);
            int y = (int)(particleAmount * 0.25f);
            _particles.Amount = Math.Max(20, (int)GD.Randi() % (x + y));

            _particles.Emitting = true;
        }
    }
}
