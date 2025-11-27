using System;
using Effects;
using Godot;
using Items;

[GlobalClass]
public partial class DescriptionPanel : PanelContainer
{
    private RichTextLabel _descLabel;

    public RichTextLabel DescriptionLabel => _descLabel;

    public override void _Ready()
    {
        _descLabel = GetNode<RichTextLabel>("%DescriptionLabel");
        base._Ready();
    }

    public void DisplayItemDescription(Item item)
    {
        if (item != null)
        {
            string descString = item.Description + "\n";
            foreach (StatEffect statEffect in item.GetEffectList())
            {
                descString += statEffect.GetEffectText() + "\n";
            }

            descString.TrimEnd('\n');
            _descLabel.Text = descString;
        }
    }

    public void DisplayString(string text)
    {
        _descLabel.Text = text;
    }
}
