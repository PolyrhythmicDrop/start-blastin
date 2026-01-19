using DataStructures;
using Godot;

[GlobalClass]
public partial class PhaseBar : PanelContainer
{
    private ProgressBar _bar;
    private RichTextLabel _label;
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
        _label = GetNode<RichTextLabel>("%PhaseLabel");
        _bgStyleBox = _bar.GetThemeStylebox("background") as StyleBoxFlat;
        _fillStyleBox = _bar.GetThemeStylebox("fill") as StyleBoxFlat;

        _bar.ValueChanged += OnBarValueChanged;
    }

    public void InitializePhaseBar(double value, double maxValue)
    {
        _bar.Value = value;
        _bar.MaxValue = maxValue;
        SetPhaseBarText();
        // Set initial colors
        _fillStyleBox.BgColor = SetFillColor();
        // _bar.AddThemeColorOverride("font_color", SetLabelColor());
        _label.AddThemeColorOverride("default_color", SetLabelColor());
    }

    /// <summary>
    /// Sets the max value of the phase progress bar to a cooldown value.
    /// </summary>
    /// <param name="totalCooldown">The new phase total cooldown value.</param>
    /// <param name="phaseReady">Whether or not the player's phase cooldown timer is currently active. If true, cooldown timer is inactive and phasing is ready.</param>
    public void SetTotalCooldown(float totalCooldown, bool phaseReady)
    {
        _bar.MaxValue = totalCooldown;
        if (phaseReady)
        {
            _bar.Value = _bar.MaxValue;
        }
        TweenPhaseBar();
    }

    /// <summary>
    /// Updates the player's phase bar with the remaining cooldown time.
    /// </summary>
    /// <param name="timeLeft">The time left on the cooldown timer.</param>
    /// <param name="totalCooldown">The total time on the cooldown timer.</param>
    public void SetPhaseCooldownTimeLeft(double timeLeft, double totalCooldown)
    {
        // double barValue = _bar.MaxValue - timeLeft;
        // if (barValue <= 0)
        // {
        //     barValue = _bar.MaxValue;
        // }
        // _bar.Value = _bar.MaxValue - timeLeft;
        if (_bar.MaxValue != totalCooldown)
        {
            _bar.MaxValue = totalCooldown;
        }
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

    private void SetPhaseBarText()
    {
        _label.Text = $"{_bar.Value / _bar.MaxValue:P0}";
    }

    private void OnBarValueChanged(double value)
    {
        SetPhaseBarText();
    }

    private void TweenPhaseBar()
    {
        double currentValue = _bar.Value;
        double maxValue = _bar.MaxValue;

        // Store the old colors
        Color currentLabelColor = _label.GetThemeColor("default_color");
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
                            _label.AddThemeColorOverride("default_color", color);
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
