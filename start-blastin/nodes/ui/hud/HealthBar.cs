using System;
using DataStructures;
using Factories;
using Godot;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class HealthBar : PanelContainer
    {
        private ProgressBar _bar;
        private RichTextLabel _label;
        private Tween _tween;

        private ColorRange _textColorRange;
        private ColorRange _barColorRange;

        [Export]
        public ColorRange TextColors
        {
            get => _textColorRange;
            set => _textColorRange = value;
        }

        [Export]
        public ColorRange BarColors
        {
            get => _barColorRange;
            set => _barColorRange = value;
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
        public void InitializeHealthBar(double value, double maxValue)
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
            TweenCurrentHealth(_bar.Value, maxHealth);
        }

        /// <summary>
        /// Sets the current value of the health bar.
        /// </summary>
        /// <param name="currentHealth"></param>
        public void SetCurrentHealth(double currentHealth, float diff)
        {
            currentHealth = Math.Ceiling(currentHealth);
            Vector2 indPos = _bar.GlobalPosition + (_bar.GetGlobalRect().Size / 3);
            IndicatorFactory.CreateTextIndicator(
                MathF.Round(diff, 1).ToString(),
                indPos,
                parent: _bar
            );
            TweenCurrentHealth(currentHealth, _bar.MaxValue);
        }

        private void SetHealthLabelText() => SetHealthLabelText(_bar.Value, _bar.MaxValue);

        /// <summary>
        /// Sets the health label text using a Vector2 for tweening.
        /// </summary>
        /// <param name="health">The health vector. X is the current health (or Value), Y is the max health (or MaxValue).</param>
        private void SetHealthLabelText(Vector2 health) => SetHealthLabelText(health.X, health.Y);

        private void SetHealthLabelText(double currentHealth, double maxHealth)
        {
            _label.Text = $"{currentHealth:N0} / {maxHealth:N0}";
        }

        /// <summary>
        /// Sets the color of the health label text according to the percent of the player's remaining health.
        /// </summary>
        /// <param name="currentHealth">The displayed value of the player's current health.</param>
        /// <param name="maxHealth">The displayed value of the player max health.</param>
        /// <returns>The <see cref="Color"/> the health label should be.</returns>
        private Color SetHealthLabelColor(double currentHealth, double maxHealth)
        {
            // Set the color according to the percentage.
            float percent = (float)(currentHealth / maxHealth);
            Color newColor = percent switch
            {
                >= 0.8f => _textColorRange.Full,
                > 0.4f => _textColorRange.Mid,
                > 0f => _textColorRange.Low,
                _ => _textColorRange.Low, // fallback for 0 or negative
            };

            return newColor;
        }

        /// <summary>
        /// Sets the fill color of the health progress bar according to the percent of the player's remaining health.
        /// </summary>
        /// <param name="currentHealth">The displayed value of the player's current health.</param>
        /// <param name="maxHealth">The displayed value of the player max health.</param>
        /// <returns>The <see cref="Color"/> the health progress bar should be.</returns>
        private Color SetBarColor(double currentHealth, double maxHealth)
        {
            // Set the color according to the percentage.
            float percent = (float)(currentHealth / maxHealth);
            Color newColor = percent switch
            {
                >= 0.8f => _barColorRange.Full,
                > 0.4f => _barColorRange.Mid,
                > 0f => _barColorRange.Low,
                _ => _barColorRange.Low, // fallback for 0 or negative
            };

            return newColor;
        }

        private void TweenCurrentHealth(double newHealth, double newMaxHealth)
        {
            double currentValue = _bar.Value;
            double currentMaxValue = _bar.MaxValue;

            Vector2 currentHealthVector = new Vector2((float)currentValue, (float)currentMaxValue);
            Vector2 newHealthVector = new Vector2((float)newHealth, (float)newMaxHealth);

            // Kill any existing tween
            if (_tween != null)
            {
                _tween.Kill();
            }

            // Get the appropriate colors
            Color newLabelColor = SetHealthLabelColor(newHealth, newMaxHealth);
            Color currentLabelColor = _label.GetThemeColor("default_color", "RichTextLabel");

            Color newBarColor = SetBarColor(newHealth, newMaxHealth);
            StyleBoxFlat barStyleBox = _bar.GetThemeStylebox("fill") as StyleBoxFlat;
            Color currentBarColor = barStyleBox.BgColor;

            // Create a new tween
            _tween = CreateTween();
            _tween.SetParallel(true);
            // Tween the progress bar for current health
            if (newHealth != currentValue)
            {
                _tween
                    .TweenProperty(_bar, "value", newHealth, 0.6)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
            }
            // Tween the progress bar for max health
            if (newMaxHealth != currentMaxValue)
            {
                _tween
                    .TweenProperty(_bar, "max_value", newMaxHealth, 0.6)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Sine);
            }
            // Tween the text
            _tween
                .TweenMethod(
                    Callable.From(
                        (Vector2 currentHealth) =>
                        {
                            SetHealthLabelText(currentHealth);
                        }
                    ),
                    currentHealthVector,
                    newHealthVector,
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
