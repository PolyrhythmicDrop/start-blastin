using Autoloads;
using Godot;

public partial class WaveStartUi : CanvasLayer
{
    public Control Control { get; set; }
    public Button WaveStartButton { get; set; }

    public override void _Ready()
    {
        Control = GetNode<Control>("%Control");
        WaveStartButton = GetNode<Button>("%Control/WaveStartButton");

        WaveStartButton.Pressed += () =>
        {
            EventBus.Instance.EmitSignal(EventBus.SignalName.StartWaveButtonPressed);
        };

        EventBus.Instance.SpawnersReady += () =>
        {
            GD.Print(
                $"{System.Reflection.MethodBase.GetCurrentMethod().ReflectedType}: Spawners ready signal received from event bus!"
            );
            Visible = true;
            Control.Visible = true;
            WaveStartButton.Visible = true;
        };

        EventBus.Instance.WaveStarted += (wave) =>
        {
            Visible = false;
            Control.Visible = false;
            WaveStartButton.Visible = false;
        };
    }
}
