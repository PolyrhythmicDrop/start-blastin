using System.Collections.Generic;
using Autoloads;
using Events;
using Godot;
using Interfaces;

namespace Environmental
{
    [GlobalClass]
    public partial class LevelCamera : Camera2D, IListener
    {
        private FastNoiseLite _noise;
        private Vector2 _defaultOffset;
        private Tween _shakeTween;

        [Export]
        public FastNoiseLite Noise
        {
            get => _noise;
            set => _noise = value;
        }

        public override void _Ready()
        {
            _defaultOffset = Offset;
            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerTakeDamage += OnPlayerTakeDamage;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerTakeDamage -= OnPlayerTakeDamage;
        }

        private void OnPlayerTakeDamage(object source, PlayerTakeDamageEventArgs args)
        {
            ShakeCamera(args.Damage);
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
