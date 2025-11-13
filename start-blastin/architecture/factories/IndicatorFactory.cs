using System;
using Godot;

namespace Factories
{
    public static class IndicatorFactory
    {
        private static PackedScene _textIndicatorScene =>
            GD.Load<PackedScene>("uid://fj4gemo0sbhx");

        public static TextIndicator CreateTextIndicator(float value, Vector2 globalPosition)
        {
            TextIndicator indicator = _textIndicatorScene.Instantiate<TextIndicator>();
            indicator.Value = value;
            indicator.GlobalPosition = globalPosition;
            return indicator;
        }
    }
}
