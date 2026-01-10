using System;
using Godot;

namespace BackgroundGenerator
{
    public partial class CelestialBody : Node2D
    {
        protected Sprite2D _sprite;

        public Sprite2D Sprite => _sprite;

        protected float _minScale = 1.0f;
        protected float _maxScale = 1.0f;

        protected VisibleOnScreenNotifier2D _visibleNotifier = new VisibleOnScreenNotifier2D();

        public float MinScale => _minScale;
        public float MaxScale => _maxScale;

        public VisibleOnScreenNotifier2D VisibleNotifier => _visibleNotifier;

        protected float _minSpeed;
        protected float _maxSpeed;

        protected float _setSpeed;

        public float MinSpeed => _minSpeed;
        public float MaxSpeed => _maxSpeed;

        public float SetSpeed
        {
            get => _setSpeed;
            set => _setSpeed = Math.Clamp(value, _minSpeed, _maxSpeed);
        }

        public override void _Ready()
        {
            AddVisibleNotifier();
        }

        public bool IsOnScreen()
        {
            return _visibleNotifier?.IsOnScreen() ?? false;
        }

        protected virtual void AddVisibleNotifier()
        {
            // Get the size of the object.
            Rect2 rect = _sprite.GetRect();
            AddChild(_visibleNotifier);
            _visibleNotifier.Rect = rect;

            _visibleNotifier.ShowRect = false;
        }

        public override void _Process(double delta)
        {
            MoveLocalY(_setSpeed * (float)delta, false);
        }
    }
}
