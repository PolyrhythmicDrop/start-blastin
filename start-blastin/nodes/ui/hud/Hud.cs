using System;
using Autoloads;
using Godot;

[GlobalClass]
public partial class Hud : Control
{
    private int _playerId;
    private StaticBody2D _hudBody;
    private CollisionShape2D _hudCollision;
    private PanelContainer _baseContainer;
    private PanelContainer _wavePanel;
    private Label _waveCounter;
    private PanelContainer _waveProgressContainer;
    private Control _waveProgressControl;
    private TextureProgressBar _waveProgressBar;
    private Label _waveProgressLabel;

    public void Initialize(int playerId)
    {
        _playerId = playerId;
    }

    public override void _Ready()
    {
        _hudBody = GetNode<StaticBody2D>("%HUDBody");
        _hudCollision = GetNode<CollisionShape2D>("%HUDCollision");
        _baseContainer = GetNode<PanelContainer>("%BaseContainer");
        _wavePanel = GetNode<PanelContainer>("%WavePanel");
        _waveCounter = GetNode<Label>("%WaveCounter");
        _waveProgressControl = GetNode<Control>("%WaveProgressControl");
        _waveProgressBar = GetNode<TextureProgressBar>("%WaveProgressBar");
        _waveProgressLabel = GetNode<Label>("%WaveProgressLabel");

        ConnectSignals();
    }

    public void ConnectSignals()
    {
        // Connect the shape re-sizer.
        Callable shapeResizeCallable = Callable.From(SetCollisionShape);
        if (!IsConnected(Control.SignalName.Resized, shapeResizeCallable))
        {
            Connect(Control.SignalName.Resized, shapeResizeCallable);
        }

        // Connect the wave count signal
        Callable waveCountCallable = Callable.From((int count) => SetWaveCount(count));
        if (!EventBus.Instance.IsConnected(EventBus.SignalName.WaveStarted, waveCountCallable))
        {
            EventBus.Instance.Connect(EventBus.SignalName.WaveStarted, waveCountCallable);
        }

        // Connect the wave time left signal for the progress bar
        Callable waveTimeLeftCallable = Callable.From(
            (float timeLeft) => SetWaveProgress(timeLeft)
        );
        if (!EventBus.Instance.IsConnected(EventBus.SignalName.WaveTimeLeft, waveTimeLeftCallable))
        {
            EventBus.Instance.Connect(EventBus.SignalName.WaveTimeLeft, waveTimeLeftCallable);
        }
    }

    private void SetCollisionShape()
    {
        RectangleShape2D newShape = new() { Size = Size };
        _hudCollision.Shape = newShape;
        _hudCollision.Position = Size / 2;
    }

    private void SetWaveCount(int waveCount)
    {
        _waveCounter.Text = $"Wave {waveCount}";
    }

    private void SetWaveProgress(float timeLeft)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeLeft);
        // _waveProgressLabel.Text = $"{time.Minutes}:{time.Seconds}";
        _waveProgressLabel.Text = time.ToString("mm':'ss");
    }
}
