using System;
using System.Numerics;
using Godot;

namespace UI.HUD
{
    [GlobalClass]
    public partial class HealthBar : PanelContainer
    {
        private ProgressBar _bar;
        private RichTextLabel _label;
        private Color _fullHealthColor;
        private Color _midHealthColor;
        private Color _lowHealthColor;

        [ExportCategory("Colors")]
        [Export]
        public Color FullHealthColor
        {
            get => _fullHealthColor;
            set => _fullHealthColor = value;
        }

        [Export]
        public Color MidHealthColor
        {
            get => _midHealthColor;
            set => _midHealthColor = value;
        }

        [Export]
        public Color LowHealthColor
        {
            get => _lowHealthColor;
            set => _lowHealthColor = value;
        }

        public override void _Ready()
        {
            _bar = GetNode<ProgressBar>("%HealthBar");
            _label = GetNode<RichTextLabel>("%HealthLabel");
        }

        public void InitializeHealthBar(double maxValue, double value)
        {
            _bar.MaxValue = maxValue;
            _bar.Value = value;
            SetHealthLabelText();
        }

        public void SetMaxHealth(double maxHealth)
        {
            _bar.MaxValue = maxHealth;
            SetHealthLabelText();
        }

        public void SetCurrentHealth(double currentHealth)
        {
            double oldHealth = _bar.Value;
            // _bar.Value = currentHealth;
            TweenCurrentHealth(oldHealth, currentHealth);
        }

        private void SetHealthLabelText() => SetHealthLabelText(_bar.Value, _bar.MaxValue);

        private void SetHealthLabelText(double currentHealth, double maxHealth)
        {
            _label.Text = $"{currentHealth:N0} / {maxHealth}";
            SetHealthLabelColor(currentHealth, maxHealth);
        }

        private void SetHealthLabelColor(double currentHealth, double maxHealth)
        {
            // Set the color according to the percentage.
            float percent = (float)(currentHealth / maxHealth);
            Color newColor = percent switch
            {
                >= 0.8f => _fullHealthColor,
                > 0.4f => _midHealthColor,
                > 0f => _lowHealthColor,
                _ => _lowHealthColor, // fallback for 0 or negative
            };

            Color currentColor = _label.GetThemeColor("default_color", "RichTextLabel");
            if (currentColor != newColor)
            {
                // _healthLabel.AddThemeColorOverride("default_color", color);

                // Tween the color values
                Tween colorTween = _label.CreateTween();
                colorTween
                    .TweenMethod(
                        Callable.From(
                            (Color color) =>
                            {
                                _label.AddThemeColorOverride("default_color", color);
                            }
                        ),
                        currentColor,
                        newColor,
                        0.5
                    )
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Expo);
                ;
            }
        }

        private void TweenCurrentHealth(double oldHealth, double newHealth)
        {
            Tween barTween = _bar.CreateTween();
            barTween.SetParallel(true);
            barTween
                .TweenProperty(_bar, "value", newHealth, 0.8)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            barTween
                .TweenMethod(
                    Callable.From(
                        (double currentHealth) =>
                        {
                            SetHealthLabelText(currentHealth, _bar.MaxValue);
                        }
                    ),
                    oldHealth,
                    newHealth,
                    0.8
                )
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Expo);
            ;
        }
    }
}
