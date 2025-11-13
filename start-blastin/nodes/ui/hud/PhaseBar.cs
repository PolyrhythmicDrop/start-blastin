using System.Runtime.Serialization.Formatters;
using DataStructures;
using Godot;
using Utility;

[GlobalClass]
public partial class PhaseBar : PanelContainer
{
    private ProgressBar _bar;
    private ColorRange _textColorRange;
    private ColorRange _barColorRange;
    private StyleBoxFlat _bgStyleBox;
    private StyleBoxFlat _fillStyleBox;
    private Tween _tween;

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
        _bar = GetNode<ProgressBar>("%PhaseBar");
        _bgStyleBox = _bar.GetThemeStylebox("background") as StyleBoxFlat;
        _fillStyleBox = _bar.GetThemeStylebox("fill") as StyleBoxFlat;
    }

    public void InitializePhaseBar(double value, double maxValue)
    {
        _bar.Value = value;
        _bar.MaxValue = maxValue;

        // Set initial colors
        _fillStyleBox.BgColor = SetFillColor();
        _bar.AddThemeColorOverride("font_color", SetLabelColor());
    }

    /// <summary>
    /// Sets the max value of the phase progress bar to a cooldown value.
    /// </summary>
    /// <param name="totalCooldown">The new phase total cooldown value.</param>
    public void SetTotalCooldown(float totalCooldown)
    {
        _bar.MaxValue = totalCooldown;
        TweenPhaseBar();
    }

    /// <summary>
    /// Updates the player's phase bar with the remaining cooldown time.
    /// </summary>
    /// <param name="timeLeft">The time left on the cooldown timer.</param>
    public void SetPhaseTimeLeft(double timeLeft)
    {
        _bar.Value = _bar.MaxValue - timeLeft;
        TweenPhaseBar();
    }

    private Color SetLabelColor()
    {
        float percent = (float)(_bar.Value / _bar.MaxValue);
        Color newColor = percent switch
        {
            >= 0.8f => _textColorRange.Full,
            > 0.4f => _textColorRange.Mid,
            > 0f => _textColorRange.Low,
            _ => _textColorRange.Low, // fallback for 0 or negative
        };
        return newColor;
    }

    private Color SetFillColor()
    {
        float percent = (float)(_bar.Value / _bar.MaxValue);
        Color newColor = percent switch
        {
            >= 0.8f => _barColorRange.Full,
            > 0.4f => _barColorRange.Mid,
            > 0f => _barColorRange.Low,
            _ => _barColorRange.Low, // fallback for 0 or negative
        };
        return newColor;
    }

    private void TweenPhaseBar()
    {
        double currentValue = _bar.Value;
        double maxValue = _bar.MaxValue;

        // Store the old colors
        Color currentLabelColor = _bar.GetThemeColor("font_color");
        Color currentFillColor = _fillStyleBox.BgColor;

        // Get the new colors
        Color newLabelColor = SetLabelColor();
        Color newFillColor = SetFillColor();

        if (currentLabelColor == newLabelColor && currentFillColor == newFillColor)
        {
            return;
        }
        else
        {
            // Kill any existing tween
            if (_tween != null)
            {
                _tween.Kill();
            }
            // Create the tween
            _tween = CreateTween();
            _tween.SetParallel(true);
            // Tween the label color
            if (currentLabelColor != newLabelColor)
            {
                _tween.TweenMethod(
                    Callable.From(
                        (Color color) =>
                        {
                            _bar.AddThemeColorOverride("font_color", color);
                        }
                    ),
                    currentLabelColor,
                    newLabelColor,
                    0.4
                );
            }
            // Tween the fill color
            if (currentFillColor != newFillColor)
            {
                _tween.TweenProperty(_fillStyleBox, "bg_color", newFillColor, 0.4);
            }
        }
    }
}
