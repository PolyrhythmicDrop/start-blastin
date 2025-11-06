using System;
using Effects;
using Godot;

[GlobalClass]
public partial class StatLabel : RichTextLabel
{
    public void SetEffectText(StatEffect effect)
    {
        string type = $"[b]{effect.Type}[/b]";
        string valueColor = effect.Value > 0 ? "#25bcc6" : "#ff5470";

        string fxText = $"{type} => [color={valueColor}]{effect.Value}[/color]";

        Text = fxText;
    }
}
