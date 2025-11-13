using Autoloads;
using Entities;
using Events;
using Factories;
using Godot;
using Interfaces;
using Services;
using UI;
using Utility;

[GlobalClass]
public partial class CurrencyPanel : PanelContainer, IListener
{
    private int _playerId;
    private PlayerService _service;
    private Label _bytesLabel;
    private Label _fluxLabel;
    private TextureRect _fluxIcon;

    public override void _Ready()
    {
        _bytesLabel = GetNode<Label>("%BytesLabel");
        _fluxLabel = GetNode<Label>("%FluxLabel");
        _fluxIcon = GetNode<TextureRect>("%FluxIcon");
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
        UpdateFlux(args.PlayerId, args.TotalFlux, args.FluxChange);
        UpdateBytes(args.PlayerId, args.TotalBytes, args.BytesChange);
    }

    private void UpdateFlux(int playerId, int totalFlux, int fluxChange)
    {
        if (playerId == _playerId && fluxChange != 0)
        {
            DebugLogger.LogMessage($"Flux updated!", true);
            // Spawn an indicator
            TextIndicator indicator = IndicatorFactory.CreateTextIndicator(
                fluxChange,
                _fluxLabel.GlobalPosition
            );
            UiLayer ui = UiLayer.GetUiLayer(_playerId);
            ui.AddChild(indicator);
            // Set the label text
            _fluxLabel.Text = totalFlux.ToString();
        }
    }

    private void UpdateBytes(int playerId, int totalBytes, int bytesChange)
    {
        if (playerId == _playerId)
        {
            // DebugLogger.LogMessage(
            //     $"Updating bytes in HUD! ID: {playerId} | Bytes: {totalBytes} | Bytes change: {bytesChange}"
            // );
            _bytesLabel.Text = totalBytes.ToString();
        }
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
