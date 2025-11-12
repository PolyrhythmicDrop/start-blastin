using System;
using System.Numerics;
using System.Runtime.Serialization;
using Godot;

namespace UI.HUD
{
    [GlobalClass]
    public partial class HealthBar : PanelContainer
    {
        private ProgressBar _bar;
        private RichTextLabel _label;
        private Color _fullHealthTextColor;
        private Color _midHealthTextColor;
        private Color _lowHealthTextColor;

        private Color _fullHealthBarColor;
        private Color _midHealthBarColor;
        private Color _lowHealthBarColor;

        private Tween _tween;

        [ExportCategory("Text Colors")]
        [Export]
        public Color FullHealthTextColor
        {
            get => _fullHealthTextColor;
            set => _fullHealthTextColor = value;
        }

        [Export]
        public Color MidHealthTextColor
        {
            get => _midHealthTextColor;
            set => _midHealthTextColor = value;
        }

        [Export]
        public Color LowHealthTextColor
        {
            get => _lowHealthTextColor;
            set => _lowHealthTextColor = value;
        }

        [ExportCategory("Bar Colors")]
        [Export]
        public Color FullHealthBarColor
        {
            get => _fullHealthBarColor;
            set => _fullHealthBarColor = value;
        }

        [Export]
        public Color MidHealthBarColor
        {
            get => _midHealthBarColor;
            set => _midHealthBarColor = value;
        }

        [Export]
        public Color LowHealthBarColor
        {
            get => _lowHealthBarColor;
            set => _lowHealthBarColor = value;
        }

        public override void _Ready()
        {
            _bar = GetNode<ProgressBar>("%HealthBar");
            _label = GetNode<RichTextLabel>("%HealthLabel");
        }

        /// <summary>
        /// Initializes the health bar values and colors.
        /// </summary>
        /// <param name="maxValue">The player's max health, the maximum value of the health bar.</param>
        /// <param name="value">The player's current health, the current value of the health bar.</param>
        public void InitializeHealthBar(double maxValue, double value)
        {
            _bar.MaxValue = maxValue;
            _bar.Value = value;
            SetHealthLabelText();
            // Set the initial colors
            _label.AddThemeColorOverride("default_color", SetHealthLabelColor(value, maxValue));
            StyleBoxFlat stylebox = _bar.GetThemeStylebox("fill") as StyleBoxFlat;
            stylebox.BgColor = SetBarColor(value, maxValue);
        }

        /// <summary>
        /// Sets the max value of the health bar.
        /// </summary>
        /// <param name="maxHealth"></param>
        public void SetMaxHealth(double maxHealth)
        {
            _bar.MaxValue = maxHealth;
            SetHealthLabelText();
        }

        /// <summary>
        /// Sets the current value of the health bar.
        /// </summary>
        /// <param name="currentHealth"></param>
        public void SetCurrentHealth(double currentHealth)
        {
            currentHealth = Math.Max(0, currentHealth);
            TweenCurrentHealth(currentHealth);
        }

        private void SetHealthLabelText() => SetHealthLabelText(_bar.Value, _bar.MaxValue);

        private void SetHealthLabelText(double currentHealth, double maxHealth)
        {
            _label.Text = $"{currentHealth:N0} / {maxHealth}";
        }

        private Color SetHealthLabelColor(double currentHealth, double maxHealth)
        {
            // Set the color according to the percentage.
            float percent = (float)(currentHealth / maxHealth);
            Color newColor = percent switch
            {
                >= 0.8f => _fullHealthTextColor,
                > 0.4f => _midHealthTextColor,
                > 0f => _lowHealthTextColor,
                _ => _lowHealthTextColor, // fallback for 0 or negative
            };

            return newColor;
        }

        private Color SetBarColor(double currentHealth, double maxHealth)
        {
            // Set the color according to the percentage.
            float percent = (float)(currentHealth / maxHealth);
            Color newColor = percent switch
            {
                >= 0.8f => _fullHealthBarColor,
                > 0.4f => _midHealthBarColor,
                > 0f => _lowHealthBarColor,
                _ => _lowHealthBarColor, // fallback for 0 or negative
            };

            return newColor;
        }

        private void TweenCurrentHealth(double newHealth)
        {
            double currentValue = _bar.Value;
            double currentMaxValue = _bar.MaxValue;

            // Kill any existing tween
            if (_tween != null)
            {
                _tween.Kill();
            }

            // Get the appropriate colors
            Color newLabelColor = SetHealthLabelColor(newHealth, _bar.MaxValue);
            Color currentLabelColor = _label.GetThemeColor("default_color", "RichTextLabel");

            Color newBarColor = SetBarColor(newHealth, _bar.MaxValue);
            StyleBoxFlat barStyleBox = _bar.GetThemeStylebox("fill") as StyleBoxFlat;
            Color currentBarColor = barStyleBox.BgColor;

            // Create a new tween
            _tween = CreateTween();
            _tween.SetParallel(true);
            // Tween the progress bar
            _tween
                .TweenProperty(_bar, "value", newHealth, 0.8)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            // Tween the text
            _tween
                .TweenMethod(
                    Callable.From(
                        (double currentHealth) =>
                        {
                            SetHealthLabelText(currentHealth, _bar.MaxValue);
                        }
                    ),
                    currentValue,
                    newHealth,
                    0.4
                )
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            ;
            // Tween the text color if the new color is different.
            if (newLabelColor != currentLabelColor)
            {
                _tween.TweenMethod(
                    Callable.From(
                        (Color color) =>
                        {
                            _label.AddThemeColorOverride("default_color", color);
                        }
                    ),
                    currentLabelColor,
                    newLabelColor,
                    0.4
                );
            }
            // Tween the bar color if the new color is different.
            if (newBarColor != currentBarColor)
            {
                _tween.TweenProperty(barStyleBox, "bg_color", newBarColor, 0.4);
            }
        }
    }
}
