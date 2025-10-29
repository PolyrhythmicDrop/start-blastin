using System;
using Godot;

[GlobalClass]
public partial class CurrencyPanel : PanelContainer
{
    private int _playerId;
    private Label _bytesLabel;
    private Label _fluxLabel;

    public override void _Ready()
    {
        _bytesLabel = GetNode<Label>("%BytesLabel");
        _fluxLabel = GetNode<Label>("%FluxLabel");
    }

    public void Initialize(int playerId)
    {
        _playerId = playerId;
    }
}
