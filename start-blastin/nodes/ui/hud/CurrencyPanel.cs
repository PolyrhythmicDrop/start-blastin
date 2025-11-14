using Autoloads;
using Entities;
using Events;
using Factories;
using Godot;
using Interfaces;
using Services;
using UI;

[GlobalClass]
public partial class CurrencyPanel : PanelContainer, IListener
{
    private int _playerId;
    private PlayerService _service;
    private Label _bytesLabel;
    private Tween _bytesTween;
    private Label _fluxLabel;
    private Tween _fluxTween;
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
            // Spawn an indicator
            Vector2 centerPos = _fluxLabel.GlobalPosition + (_fluxLabel.Size / 2);
            TextIndicator indicator = IndicatorFactory.CreateTextIndicator(
                fluxChange,
                globalPosition: centerPos
            );
            UiLayer ui = UiLayer.GetUiLayer(_playerId);
            ui.AddChild(indicator);
            // Set the label text
            TweenFluxLabelText(totalFlux);
        }
    }

    private void SetFluxLabel(int value)
    {
        _fluxLabel.Text = $"{value:N0}";
    }

    private void TweenFluxLabelText(int finalValue)
    {
        if (_fluxTween != null)
        {
            _fluxTween.Kill();
        }
        int ogValue = _fluxLabel.Text.ToInt();
        _fluxTween = CreateTween();
        _fluxTween.TweenMethod(
            Callable.From((int value) => SetFluxLabel(value)),
            ogValue,
            finalValue,
            0.5
        );
    }

    private void UpdateBytes(int playerId, int totalBytes, int bytesChange)
    {
        if (playerId == _playerId && bytesChange != 0)
        {
            // Spawn an indicator
            Vector2 centerPos = _bytesLabel.GlobalPosition + (_bytesLabel.Size / 2);
            TextIndicator indicator = IndicatorFactory.CreateTextIndicator(bytesChange, centerPos);
            UiLayer ui = UiLayer.GetUiLayer(_playerId);
            ui.AddChild(indicator);
            // Set the label text
            TweenByteLabelText(totalBytes);
        }
    }

    private void SetBytesLabel(int value)
    {
        _bytesLabel.Text = $"{value:N0}";
    }

    private void TweenByteLabelText(int finalValue)
    {
        if (_bytesTween != null)
        {
            _bytesTween.Kill();
        }
        int ogValue = _bytesLabel.Text.ToInt();
        _bytesTween = CreateTween();
        _bytesTween.TweenMethod(
            Callable.From((int value) => SetBytesLabel(value)),
            ogValue,
            finalValue,
            0.5
        );
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
