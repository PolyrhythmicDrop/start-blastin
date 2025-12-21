using System;
using System.Text;
using System.Text.RegularExpressions;
using Autoloads;
using DataStructures;
using Godot;
using Utility;

[GlobalClass]
public partial class TextIndicator : Node2D
{
    private RichTextLabel _label;
    private string _value;
    private ColorRange _colors;
    private Tween _tween;
    private int _fontSize;

    [Export]
    public ColorRange Colors
    {
        get => _colors;
        set => _colors = value;
    }

    [Export]
    public string Value
    {
        get => _value;
        set => _value = value;
    }

    [Export]
    public int FontSize
    {
        get => _fontSize;
        set => _fontSize = Math.Max(1, value);
    }

    public override void _Ready()
    {
        _label = GetNode<RichTextLabel>("%RichTextLabel");
        SetLabelFontSize();
        SetLabel();
        Animate();
    }

    public void SetLabelColor(Color color)
    {
        _label.AddThemeColorOverride("default_color", color);
    }

    private void SetLabelFontSize()
    {
        _label.AddThemeFontSizeOverride("normal_font_size", FontSize);
    }

    private void SetLabel()
    {
        if (ExtractAndFormatNumberString(_value, out string output))
        {
            if (output.Contains('-'))
            {
                SetLabelColor(_colors.Low);
            }
            else if (output.Contains('+'))
            {
                SetLabelColor(_colors.Full);
            }
            else
            {
                SetLabelColor(_colors.Mid);
            }
        }
        _label.Text = $"{output}";
    }

    private bool ExtractAndFormatNumberString(string inputStr, out string outputStr)
    {
        Match match = Regex.Match(inputStr, @"-?\d+\.?\d*");
        if (match.Success && float.TryParse(match.Value, out float value))
        {
            char sign;
            if (value >= 0)
            {
                sign = '+';
            }
            else
            {
                sign = '-';
            }

            outputStr = inputStr.Replace(match.Value, $"{sign}{Math.Abs(value)}");
            return true;
        }
        else
        {
            outputStr = inputStr;
            return false;
        }
    }

    private void Animate()
    {
        float finalYPos = GlobalPosition.Y - (float)RNG.GetRandomDouble(20, 40);
        _tween = CreateTween();
        _tween
            .TweenProperty(this, "global_position:y", finalYPos, 0.5)
            .SetTrans(Tween.TransitionType.Spring)
            .SetEase(Tween.EaseType.Out);
        _tween.Parallel().TweenProperty(this, "scale", new Vector2(0.5f, 0.5f), 1);
        _tween.Parallel().TweenProperty(this, "modulate:a", 0, 0.5);
        _tween.TweenCallback(Callable.From(QueueFree));
    }
}
