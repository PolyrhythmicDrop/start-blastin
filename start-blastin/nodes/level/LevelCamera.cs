using System.Collections.Generic;
using Autoloads;
using Events;
using Godot;
using Interfaces;
using Utility;

namespace Environmental
{
    [GlobalClass]
    public partial class LevelCamera : Camera2D, IListener
    {
        // ~~ Shake variables ~~
        private FastNoiseLite _noise;
        private Vector2 _defaultOffset;
        private Tween _shakeTween;

        // ~~ Health vignette variables ~~

        private CanvasLayer _vignetteLayer;
        private ColorRect _vignetteRect;
        private ShaderMaterial _vignetteMaterial;
        private Tween _vignetteTween;
        private bool _vignetteEnabled = false;

        [Export]
        public FastNoiseLite Noise
        {
            get => _noise;
            set => _noise = value;
        }

        public override void _Ready()
        {
            _defaultOffset = Offset;

            _vignetteLayer = GetNode<CanvasLayer>("%VignetteLayer");
            _vignetteRect = _vignetteLayer.GetNode<ColorRect>("%VignetteRect");
            if (_vignetteRect.Material is ShaderMaterial shaderMaterial)
            {
                _vignetteMaterial = shaderMaterial;
            }

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerTakeDamage += OnPlayerTakeDamage;
            EventBus.Instance.PlayerCurrentHealthChanged += OnPlayerCurrentHealthChanged;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerTakeDamage -= OnPlayerTakeDamage;
            EventBus.Instance.PlayerCurrentHealthChanged -= OnPlayerCurrentHealthChanged;
        }

        private void OnPlayerTakeDamage(object source, PlayerTakeDamageEventArgs args)
        {
            ShakeCamera(args.Damage);
        }

        private void OnPlayerCurrentHealthChanged(
            object source,
            PlayerCurrentHealthChangedEventArgs args
        )
        {
            if (args.Percentage <= 0.1f && !_vignetteEnabled)
            {
                EnableLowHealthVignette();
            }
            else if (args.Percentage > 0.1f && _vignetteEnabled)
            {
                DisableLowHealthVignette();
            }
        }

        private void EnableLowHealthVignette()
        {
            _vignetteEnabled = true;
            if (_vignetteTween != null && _vignetteTween.IsValid())
            {
                _vignetteTween.Kill();
            }

            float maxIntensity = 0.45f;
            float minIntensity = 0.1f;

            Callable intensityCallable = Callable.From(
                (float i) =>
                {
                    _vignetteMaterial.SetShaderParameter("intensity", i);
                }
            );

            _vignetteTween = _vignetteRect.CreateTween();
            _vignetteTween.SetLoops();
            _vignetteTween
                .TweenMethod(intensityCallable, minIntensity, maxIntensity, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _vignetteTween
                .TweenMethod(intensityCallable, maxIntensity, minIntensity, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        private void DisableLowHealthVignette()
        {
            _vignetteEnabled = false;
            if (_vignetteTween != null && _vignetteTween.IsValid())
            {
                _vignetteTween.Kill();
            }

            Callable intensityCallable = Callable.From(
                (float i) => _vignetteMaterial.SetShaderParameter("intensity", i)
            );
            float currentIntensity = (float)_vignetteMaterial.GetShaderParameter("intensity");

            _vignetteTween = _vignetteRect.CreateTween();

            _vignetteTween
                .TweenMethod(intensityCallable, currentIntensity, 0, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        /// <summary>
        /// Shakes the camera a set number of times based on an initial input.
        /// </summary>
        /// <param name="initialInput">The initial input value for the noise generator.</param>
        private void ShakeCamera(float initialInput)
        {
            // Kill the current tween and start over if the tween is already running.
            if (_shakeTween != null && _shakeTween.IsValid())
            {
                _shakeTween.Kill();
            }

            // Set a random seed for the noise so it's different every time
            _noise.Seed = (int)GD.Randi();

            // Create the coordinate list and apply initial input
            HashSet<Vector2> shakeCoords = new();
            Vector2 shakeInput = Vector2.One * initialInput;

            // Populate the list of coordinates using the noise generator.
            for (int i = 0; i < 5; i++)
            {
                Vector2 newCoords = GetCameraShake(shakeInput.X, shakeInput.Y);
                shakeInput = newCoords;
                shakeCoords.Add(newCoords);
            }

            // Tween all of the coordinates in sequence.
            _shakeTween = CreateTween();
            foreach (Vector2 coords in shakeCoords)
            {
                _shakeTween.TweenProperty(this, "offset", coords, 0.05f);
            }
            _shakeTween.TweenProperty(this, "offset", _defaultOffset, 0.05f);
        }

        /// <summary>
        /// Returns coordinates for camera offset when given a set of input coordinates.
        /// </summary>
        /// <param name="inputX">Input used to generate the X-value of the returned noise Vector2.</param>
        /// <param name="inputY">Input used to generate the X-value of the returned noise Vector2.</param>
        /// <returns>X and Y noise coordinates as a Vector2.</returns>
        private Vector2 GetCameraShake(float inputX, float inputY)
        {
            float noiseValueX = _noise.GetNoise1D(inputX);
            float noiseValueY = _noise.GetNoise1D(inputY);
            return new Vector2(noiseValueX * 25, noiseValueY * 25);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
