using Autoloads;
using Entities;
using Events;
using Godot;
using Interfaces;
using Services;

[GlobalClass]
public partial class CurrencyPanel : PanelContainer, IListener
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

        ConnectSignals();
    }

    public void Initialize(int playerId)
    {
        _playerId = playerId;
        InitializeLabels();
    }

    public void InitializeLabels()
    {
        if (_service.HasPlayer(_playerId))
        {
            Player player = _service.GetPlayer(_playerId);
            _fluxLabel.Text = player.Flux.ToString();
            _bytesLabel.Text = player.Bytes.ToString();
        }
    }

    public void ConnectSignals()
    {
        EventBus.Instance.PlayerCurrencyChanged += OnPlayerCurrencyChanged;
    }

    public void DisconnectSignals()
    {
        EventBus.Instance.PlayerCurrencyChanged -= OnPlayerCurrencyChanged;
    }

    private void OnPlayerCurrencyChanged(object source, PlayerCurrencyChangedEventArgs args)
    {
        UpdateFlux(args.PlayerId, args.Flux);
        UpdateBytes(args.PlayerId, args.Bytes);
    }

    private void UpdateFlux(int playerId, int flux)
    {
        if (playerId == _playerId)
        {
            // DebugLogger.LogMessage($"Updating flux in HUD! ID: {playerId} | Flux: {flux}");
            _fluxLabel.Text = flux.ToString();
        }
    }

    private void UpdateBytes(int playerId, int bytes)
    {
        if (playerId == _playerId)
        {
            // DebugLogger.LogMessage($"Updating bytes in HUD! ID: {playerId} | Bytes: {bytes}");
            _bytesLabel.Text = bytes.ToString();
        }
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
