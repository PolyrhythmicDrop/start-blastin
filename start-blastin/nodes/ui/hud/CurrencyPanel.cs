using System;
using Autoloads;
using Entities;
using Godot;
using Services;

[GlobalClass]
public partial class CurrencyPanel : PanelContainer
{
    private int _playerId;
    private PlayerService _service;
    private Label _bytesLabel;
    private Label _fluxLabel;

    public override void _Ready()
    {
        _bytesLabel = GetNode<Label>("%BytesLabel");
        _fluxLabel = GetNode<Label>("%FluxLabel");
        _service = ServiceManager.Instance.GetService<PlayerService>();
    }

    public void Initialize(int playerId)
    {
        _playerId = playerId;
        InitializeLabels();
    }

    public void InitializeLabels()
    {
        if (_service.GetPlayerCurrency(_playerId, out int flux, out int bytes))
        {
            _fluxLabel.Text = flux.ToString();
            _bytesLabel.Text = bytes.ToString();
        }
    }

    private void ConnectSignals()
    {
        // Connect flux
        Callable fluxCallable = Callable.From((int id, int flux) => UpdateFlux(id, flux));
        if (!EventBus.Instance.IsConnected(EventBus.SignalName.PlayerFluxChange, fluxCallable))
        {
            EventBus.Instance.Connect(EventBus.SignalName.PlayerFluxChange, fluxCallable);
        }

        // Connect bytes
        Callable byteCallable = Callable.From((int id, int bytes) => UpdateBytes(id, bytes));
        if (!EventBus.Instance.IsConnected(EventBus.SignalName.PlayerBytesChange, byteCallable))
        {
            EventBus.Instance.Connect(EventBus.SignalName.PlayerBytesChange, byteCallable);
        }
    }

    private void UpdateFlux(int playerId, int flux)
    {
        if (playerId == _playerId)
        {
            _fluxLabel.Text = flux.ToString();
        }
    }

    private void UpdateBytes(int playerId, int bytes)
    {
        if (playerId == _playerId)
        {
            _bytesLabel.Text = bytes.ToString();
        }
    }
}
