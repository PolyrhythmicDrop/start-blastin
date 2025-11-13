using System;
using DataStructures;
using Godot;

[GlobalClass]
public partial class TextIndicator : Node2D
{
    private RichTextLabel _label;
    private float _value;
    private ColorRange _colors;

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

    public override void _Ready()
    {
        _label = GetNode<RichTextLabel>("%RichTextLabel");
        SetLabel();
    }

    public void SetLabelColor(Color color)
    {
        _label.AddThemeColorOverride("default_color", color);
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
        _label.Text = $"{sign} {_value}";
    }
}
