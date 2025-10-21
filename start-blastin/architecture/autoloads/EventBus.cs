using Godot;

namespace Autoloads
{
    public partial class EventBus : Node
    {
        public static EventBus Instance { get; private set; }

        [Signal]
        public delegate void WaveStartedEventHandler(int wave);

        [Signal]
        public delegate void WaveEndedEventHandler();

        [Signal]
        public delegate void StartWaveButtonPressedEventHandler();

        [Signal]
        public delegate void SpawnersReadyEventHandler();

        public override void _Ready()
        {
            Instance = this;
            SpawnersReady += () =>
            {
                GD.Print(
                    $"{System.Reflection.MethodBase.GetCurrentMethod().ReflectedType}: SpawnersReady signal emitted!"
                );
            };
        }
    }
}
