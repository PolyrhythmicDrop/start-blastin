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
            int ogValue = totalFlux - fluxChange;
            TweenLabelText(_fluxLabel, _fluxTween, ogValue, totalFlux);
        }
    }

    private void SetLabelText(Label label, int value)
    {
        label.Text = $"{value:N0}";
    }

    private void TweenLabelText(Label label, Tween tween, int ogValue, int finalValue)
    {
        if (tween != null)
        {
            tween.Kill();
        }
        // int ogValue = label.Text.ToInt();
        tween = CreateTween();
        tween.TweenMethod(
            Callable.From((int value) => SetLabelText(label, value)),
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
            int ogValue = totalBytes - bytesChange;
            TweenLabelText(_bytesLabel, _bytesTween, ogValue, totalBytes);
        }
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
