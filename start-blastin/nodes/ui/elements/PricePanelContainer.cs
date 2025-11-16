using System;
using Godot;

[GlobalClass]
public partial class PricePanelContainer : PanelContainer
{
    public enum PriceLabel
    {
        Bytes,
        Flux,
        Both,
    }

    // ~ Nodes ~ //
    private PanelContainer _bytePanelContainer;
    private PanelContainer _fluxPanelContainer;
    private Label _byteLabel;
    private Label _fluxLabel;

    public override void _Ready()
    {
        _bytePanelContainer = GetNode<PanelContainer>("%BytePanelContainer");
        _fluxPanelContainer = GetNode<PanelContainer>("%FluxPanelContainer");
        _byteLabel = GetNode<Label>("%ByteLabel");
        _fluxLabel = GetNode<Label>("%FluxLabel");
    }

    public void SetLabelText(PriceLabel label, string text)
    {
        switch (label)
        {
            case PriceLabel.Bytes:
                _byteLabel.Text = text;
                break;
            case PriceLabel.Flux:
                _fluxLabel.Text = text;
                break;
            case PriceLabel.Both:
                _byteLabel.Text = text;
                _fluxLabel.Text = text;
                break;
        }
    }

    public void TogglePanelVisibility(PriceLabel label, bool visible)
    {
        switch (label)
        {
            case PriceLabel.Bytes:
                _bytePanelContainer.Visible = visible;
                break;
            case PriceLabel.Flux:
                _fluxPanelContainer.Visible = visible;
                break;
            case PriceLabel.Both:
                _fluxPanelContainer.Visible = visible;
                _bytePanelContainer.Visible = visible;
                break;
        }
    }

    public void SetFontColor(PriceLabel label, Color color)
    {
        switch (label)
        {
            case PriceLabel.Bytes:
                _byteLabel.LabelSettings.FontColor = color;
                break;
            case PriceLabel.Flux:
                _fluxLabel.LabelSettings.FontColor = color;
                break;
            case PriceLabel.Both:
                _byteLabel.LabelSettings.FontColor = color;
                _fluxLabel.LabelSettings.FontColor = color;
                break;
        }
    }
}
