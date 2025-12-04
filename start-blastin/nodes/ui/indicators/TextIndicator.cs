using System;
using Autoloads;
using DataStructures;
using Godot;

[GlobalClass]
public partial class TextIndicator : Node2D
{
    private RichTextLabel _label;
    private float _value;
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
    public float Value
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
        char sign;
        if (_value >= 0)
        {
            sign = '+';
            SetLabelColor(_colors.Full);
        }
        else
        {
            sign = '-';
            SetLabelColor(_colors.Low);
        }
        _label.Text = $"{sign}{Math.Abs(_value)}";
    }

    private void Animate()
    {
        // float finalYPos = GlobalPosition.Y - GD.RandRange(20, 40);
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
