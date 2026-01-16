using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Autoloads;
using Events;
using Factories;
using FileIO;
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

        private GpuParticles2D _starParticles;
        private GpuParticles2D _starParticles2;
        private GpuParticles2D _currentParticles;

        private Parallax2D _parallax;

        private int _maxPlanets = 3;
        private int _maxBigStars = 5;
        private CanvasLayer _bodyCanvas;
        private CanvasLayer _planetCanvas;
        private CanvasLayer _bigStarCanvas;

        private List<ShaderPlanet> _shaderPlanets = new();
        private List<BigStar> _bigStars = new();

        private List<GradientTexture1D> _colorSchemes = new();

        // ~~ Spawn timers ~~
        private Timer _starSwapTimer;

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
            // Load the color schemes
            PoolLoader.LoadResourcePool(_colorSchemes, "res://resources/colors/bg-color-schemes/");

            _background = GetNode<ColorRect>("%Background");
            _spawnBlock = GetNode<Area2D>("%SpawnBlock");
            _spawnShape = _spawnBlock.GetNode<CollisionShape2D>("%SpawnShape2D");

            _starSwapTimer = GetNode<Timer>("%StarSwapTimer");
            _planetSpawnTimer = GetNode<Timer>("%ShaderPlanetSpawnTimer");
            _bigStarSpawnTimer = GetNode<Timer>("%BigStarSpawnTimer");

            RandomizeSpawnTime(_starSwapTimer);
            RandomizeSpawnTime(_planetSpawnTimer);
            RandomizeSpawnTime(_bigStarSpawnTimer);

            _starParticles = GetNode<GpuParticles2D>("%StarParticles");
            _starParticles2 = GetNode<GpuParticles2D>("%StarParticles2");

            // Set _starParticles2 to be transparent.
            _starParticles2.Modulate = new Color(_starParticles2.Modulate, a: 0);

            _currentParticles = _starParticles;

            _parallax = GetNode<Parallax2D>("%Parallax2D");

            _bodyCanvas = GetNode<CanvasLayer>("%CelestialBody-CanvasLayer");
            _bigStarCanvas = GetNode<CanvasLayer>("%BigStar-CanvasLayer");
            _planetCanvas = GetNode<CanvasLayer>("%Planet-CanvasLayer");

            GenerateStarParticles(_starParticles);

            ConnectSignals();

            _planetSpawnTimer.Start();
            _bigStarSpawnTimer.Start();
            _starSwapTimer.Start();
        }

        public void ConnectSignals()
        {
            // Connect the body spawn timers.
            _planetSpawnTimer.Timeout += () => TrySpawnBody(CelestialBodyType.ShaderPlanet);
            _bigStarSpawnTimer.Timeout += () => TrySpawnBody(CelestialBodyType.BigStar);
            _starSwapTimer.Timeout += SwapStarParticles;

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
            float factor = (float)GD.RandRange(0.05, 2);
            timer.WaitTime = Math.Clamp(factor * timer.WaitTime, 20, 600);
        }

        private void OnWaveStarted(object source, WaveStartedEventArgs args)
        {
            // Change the starfield every 3 waves.
            // bool changeStarfield = args.Wave % 3 == 0;

            // if (changeStarfield)
            // {
            //     SwapStarParticles();
            // }
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
                            int count = GD.RandRange(1, MaxPlanets - _shaderPlanets.Count);
                            for (int i = 0; i < count; i++)
                            {
                                GenerateCelestialBody(type);
                            }
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
                            int count = GD.RandRange(0, MaxBigStars - _bigStars.Count);
                            for (int i = 0; i < count; i++)
                            {
                                GenerateCelestialBody(type);
                            }
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

            // Modulate the body to make it dimmer
            body.Modulate = body.Modulate.Darkened(Math.Clamp(GD.RandRange(0, 1), 0.025f, 0.7f));

            SetBodyPosition(body);

            ConnectCelestialBodySignals(body);

            if (body is IColorScheme schemed)
            {
                schemed.ApplyColorScheme(GetColorSchemeFromPool());
            }

            body.Visible = true;
        }

        private GradientTexture1D GetColorSchemeFromPool()
        {
            if (_colorSchemes.Count <= 0 || _colorSchemes == null)
            {
                return null;
            }

            int selection = GD.RandRange(0, _colorSchemes.Count - 1);
            return _colorSchemes[selection] ?? ColorScheme;
        }

        private void SetBodyPosition(CelestialBody body)
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
            body.GlobalPosition = _spawnBlock.ToGlobal(position);

            // Get the Rect2 of the viewport and the body
            Rect2 viewportRect = GetViewport().GetVisibleRect();
            Rect2 spriteRect;
            if (body is BigStar bigStar)
            {
                // Get the size of the current frame.
                Texture2D texture = bigStar.Sprite.SpriteFrames.GetFrameTexture("default", 5);
                spriteRect = new(bigStar.Sprite.Position, texture.GetSize());
            }
            else
            {
                spriteRect = body.Sprite.GetRect();
            }

            // Determine if the body will spawn inside the viewport.
            bool intersects = spriteRect.Intersects(viewportRect);
            if (intersects)
            {
                // Move the body out of the viewport.
                body.GlobalPosition -= new Vector2(0, spriteRect.Size.Y);
            }
        }

        private ShaderPlanet GenerateShaderPlanet()
        {
            ShaderPlanet planet = CelestialBodyFactory.CreateCelestialBody<ShaderPlanet>(
                randomScale: true
            );

            planet.Visible = false;
            _planetCanvas.AddChild(planet);
            planet.GlobalPosition = _spawnBlock.GlobalPosition;
            _shaderPlanets.Add(planet);

            return planet;
        }

        private BigStar GenerateBigStar()
        {
            BigStar bigStar = CelestialBodyFactory.CreateCelestialBody<BigStar>(randomScale: true);

            bigStar.Visible = false;
            _bigStarCanvas.AddChild(bigStar);
            bigStar.GlobalPosition = _spawnBlock.GlobalPosition;
            _bigStars.Add(bigStar);

            return bigStar;
        }

        private void ConnectCelestialBodySignals(CelestialBody body)
        {
            body.VisibleNotifier.ScreenExited += () => RemoveCelestialBody(body);
        }

        private void RemoveCelestialBody(CelestialBody body)
        {
            bool removed = body switch
            {
                ShaderPlanet planet => _shaderPlanets.Remove(planet),
                BigStar bigStar => _bigStars.Remove(bigStar),
                _ => false,
            };

            if (IsInstanceValid(body) && !body.IsQueuedForDeletion() && removed)
            {
                DebugLogger.LogMessage($"Removing {body.Name}!");
                body.QueueFree();
            }
        }

        private async void GenerateStarParticles(GpuParticles2D particles)
        {
            // particles.Emitting = true;
            particles.SpeedScale = 1.0;

            var parallaxSize = _parallax.RepeatSize;
            if (particles.ProcessMaterial is ShaderMaterial particleMaterial)
            {
                particleMaterial.SetShaderParameter(
                    "emission_box_extents",
                    new Vector3(parallaxSize.X, parallaxSize.Y, 1.0f)
                );
                particleMaterial.SetShaderParameter("colorscheme", ColorScheme);
            }

            float particleAmount =
                (parallaxSize.X * parallaxSize.Y) * GD.RandRange(100, 4000) / particles.Amount;
            int x = (int)(particleAmount * 0.75f);
            int y = (int)(particleAmount * 0.25f);
            particles.Amount = Math.Clamp((int)GD.Randi() % (x + y), 20000, 40000);

            DebugLogger.LogMessage($"particle amount: {particles.Amount}");

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            particles.SpeedScale = 0.0f;
            // _particles.Emitting = false;
        }

        private void SwapStarParticles()
        {
            GpuParticles2D nextParticles;

            // Determine what the next set of particles should be.
            nextParticles = _currentParticles.Equals(_starParticles)
                ? _starParticles2
                : _starParticles;

            // Apply the next color scheme
            ColorScheme = GetColorSchemeFromPool();

            // Fade in the next set of particles and fade out the current set of particles
            Color opaque = nextParticles.Modulate;
            opaque.A = 1;

            Color transparent = _currentParticles.Modulate;
            transparent.A = 0;

            nextParticles.Modulate = new(nextParticles.Modulate, a: 0);
            GenerateStarParticles(nextParticles);
            nextParticles.Visible = true;

            Tween tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(_currentParticles, "modulate", transparent, 15);
            tween.TweenProperty(nextParticles, "modulate", opaque, 8);

            _currentParticles = nextParticles;

            RandomizeSpawnTime(_starSwapTimer);
        }
    }
}
