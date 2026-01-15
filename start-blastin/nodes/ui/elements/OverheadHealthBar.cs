using System;
using System.Diagnostics;
using DataStructures;
using Godot;
using Interfaces;
using Utility;

namespace UI
{
    [GlobalClass]
    public partial class OverheadHealthBar : Node
    {
        public IHealthful Parent { get; set; }

        public bool Active { get; set; } = true;

        private ProgressBar _bar;

        private StyleBoxFlat _barFillStyleBox;

        private ColorRange _colors = ResourceLoader.Load<ColorRange>("uid://dfmi20atcp4he");

        private float _yOffset = 0;
        private float _xOffset = 0;

        private const float BAR_HEIGHT = 5;

        public override void _Ready()
        {
            _bar = GetNode<ProgressBar>("%Bar");
            _barFillStyleBox = _bar.GetThemeStylebox("fill") as StyleBoxFlat;
        }

        public void Initialize(IHealthful parent)
        {
            Parent = parent;
            SetValues(Parent.MaxHealth, Parent.CurrentHealth);
            DebugLogger.LogMessage(
                $"Health bar for {Parent} set! Max: {Parent.MaxHealth} | Current: {Parent.CurrentHealth}"
            );
        }

        public void ToggleBarVisibility(bool show)
        {
            _bar.Visible = show;
        }

        public void ToggleActive()
        {
            Active = !Active;
            ToggleBarVisibility(Active);
        }

        public void SetSize(Vector2 spriteSize)
        {
            // Get the width of the sprite
            float w = spriteSize.X;

            // Set the offset based on the size of the sprite.
            _yOffset = (spriteSize.Y / 2) + (BAR_HEIGHT * 3);
            _xOffset = spriteSize.X / 2;

            _bar.Size = new(w, BAR_HEIGHT);
        }

        public void SetPosition(Vector2 spritePosition)
        {
            _bar.GlobalPosition = new(spritePosition.X - _xOffset, spritePosition.Y - _yOffset);
        }

        public void SetValues(float maxValue, float value)
        {
            _bar.MaxValue = maxValue;
            _bar.Value = value;
            float barPercent = value / maxValue;

            if (barPercent == 0 || barPercent == 1)
            {
                ToggleBarVisibility(false);
            }
            else if (Active && _bar.Visible == false)
            {
                ToggleBarVisibility(true);
            }

            Color barColor = barPercent switch
            {
                <= 0.25f => _colors.Low,
                < 0.5f => _colors.Mid,
                > 0.5f => _colors.Full,
                _ => _colors.Full,
            };

            SetBarColor(barColor);
        }

        public void SetBarColor(Color color)
        {
            _barFillStyleBox.BgColor = color;
        }
    }
}
