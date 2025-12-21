using System;
using Autoloads;
using Godot;
using NanoidDotNet;
using Utility;

namespace Factories
{
    public static class IndicatorFactory
    {
        private static PackedScene _textIndicatorScene = GD.Load<PackedScene>("uid://fj4gemo0sbhx");

        public static TextIndicator CreateTextIndicator(
            string value,
            Vector2 globalPosition,
            Node parent = null,
            int fontSize = 28
        )
        {
            TextIndicator indicator = _textIndicatorScene.Instantiate<TextIndicator>();
            indicator.Value = value;
            indicator.FontSize = fontSize;
            indicator.GlobalPosition = globalPosition;

            if (parent != null)
            {
                parent.AddChild(indicator);
                indicator.Name = $"{parent.Name}-TextInd{Nanoid.Generate(size: 3)}";
            }
            return indicator;
        }
    }
}
