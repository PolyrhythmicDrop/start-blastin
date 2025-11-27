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

    public enum Mode
    {
        Default,
        Shop,
        Inventory,
    }

    // ~ Nodes ~ //
    private PanelContainer _bytePanelContainer;
    private PanelContainer _fluxPanelContainer;
    private Label _byteLabel;
    private Label _fluxLabel;
    private Mode _mode = Mode.Default;

    // ~~ Resources //

    private LabelSettings _shopLabelSettings => GD.Load<LabelSettings>("uid://cnnymev36ytvv");
    private LabelSettings _inventoryLabelSettings => GD.Load<LabelSettings>("uid://26hv5cp7uaef");
    private StyleBoxFlat _shopStyleBox => GD.Load<StyleBoxFlat>("uid://dr7nqwevwop52");
    private StyleBoxFlat _inventoryStyleBox => GD.Load<StyleBoxFlat>("uid://d4be6vg4xxy5w");

    public override void _Ready()
    {
        _bytePanelContainer = GetNode<PanelContainer>("%BytePanelContainer");
        _fluxPanelContainer = GetNode<PanelContainer>("%FluxPanelContainer");
        _byteLabel = GetNode<Label>("%ByteLabel");
        _fluxLabel = GetNode<Label>("%FluxLabel");
    }

    public void SetLabelText(string text, PriceLabel label = PriceLabel.Both)
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

    public void TogglePanelVisibility(bool visible, PriceLabel label = PriceLabel.Both)
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

    public void SetFontColor(Color color, PriceLabel label = PriceLabel.Both)
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

    private void SetStyle()
    {
        RemoveThemeStyleboxOverride("panel");

        switch (_mode)
        {
            default:
            case Mode.Default:
                break;
            case Mode.Inventory:
                _byteLabel.LabelSettings = _inventoryLabelSettings;
                _fluxLabel.LabelSettings = _inventoryLabelSettings;
                AddThemeStyleboxOverride("panel", _inventoryStyleBox);
                _bytePanelContainer.AddThemeStyleboxOverride("panel", _inventoryStyleBox);
                break;
            case Mode.Shop:
                _byteLabel.LabelSettings = _shopLabelSettings;
                _fluxLabel.LabelSettings = _shopLabelSettings;
                AddThemeStyleboxOverride("panel", _shopStyleBox);
                break;
        }
    }

    public void SetMode(Mode mode)
    {
        _mode = mode;
        SetStyle();
    }
}
