using System;
using Godot;

namespace BackgroundGenerator
{
    public partial class CelestialBody : Node2D
    {
        protected Sprite2D _sprite;

        protected VisibleOnScreenNotifier2D _visibleNotifier = new VisibleOnScreenNotifier2D();

        public VisibleOnScreenNotifier2D VisibleNotifier => _visibleNotifier;

        protected const int SPEED = 200;

        public override void _Ready()
        {
            AddVisibleNotifier();
        }

        public bool IsOnScreen()
        {
            return _visibleNotifier?.IsOnScreen() ?? false;
        }

        protected void AddVisibleNotifier()
        {
            // Get the size of the object.
            Rect2 rect = _sprite.GetRect();
            AddChild(_visibleNotifier);
            _visibleNotifier.Rect = rect;

            _visibleNotifier.ShowRect = true;
        }
    }
}
