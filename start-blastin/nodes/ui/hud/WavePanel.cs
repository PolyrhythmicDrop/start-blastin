using System;
using Autoloads;
using Events;
using Godot;

namespace UI.HUD
{
    [GlobalClass]
    public partial class WavePanel : PanelContainer
    {
        private Label _waveCounter;
        private PanelContainer _waveProgressContainer;
        private Control _waveProgressControl;
        private TextureProgressBar _waveProgressBar;
        private Label _waveProgressLabel;
        private bool _progressBarInitialized;

        public override void _Ready()
        {
            _waveCounter = GetNode<Label>("%WaveCounter");
            _waveProgressControl = GetNode<Control>("%WaveProgressControl");
            _waveProgressBar = GetNode<TextureProgressBar>("%WaveProgressBar");
            _waveProgressLabel = GetNode<Label>("%WaveProgressLabel");

            ConnectSignals();
        }

        private void ConnectSignals()
        {
            // Connect the wave count signal
            EventBus.Instance.WaveStarted += OnWaveStarted;

            // Connect the wave time left signal for the progress bar
            EventBus.Instance.WaveTimeLeft += OnWaveTimeLeft;

            // Callable waveTimeLeftCallable = Callable.From(
            //     (float timeLeft, float totalTime) => SetWaveProgress(timeLeft, totalTime)
            // );
            // if (
            //     !EventBus.Instance.IsConnected(
            //         EventBus.SignalName.WaveTimeLeft,
            //         waveTimeLeftCallable
            //     )
            // )
            // {
            //     EventBus.Instance.Connect(EventBus.SignalName.WaveTimeLeft, waveTimeLeftCallable);
            // }

            // Connect the wave complete signal
            Callable waveCompleteCallable = Callable.From(OnWaveComplete);
            if (
                !EventBus.Instance.IsConnected(
                    EventBus.SignalName.WaveComplete,
                    waveCompleteCallable
                )
            )
            {
                EventBus.Instance.Connect(EventBus.SignalName.WaveComplete, waveCompleteCallable);
            }
        }

        private void OnWaveStarted(object sender, WaveStartedEventArgs args)
        {
            SetWaveCount(args.Wave);
        }

        private void SetWaveCount(int waveCount)
        {
            _waveCounter.Text = $"Wave {waveCount}";
        }

        private void OnWaveTimeLeft(object sender, WaveTimeLeftEventArgs args) =>
            SetWaveProgress(args.TimeLeft, args.TotalTime);

        private void SetWaveProgress(double timeLeft, double totalTime)
        {
            if (!_progressBarInitialized)
            {
                InitializeWaveProgressBar(totalTime);
            }
            TimeSpan time = TimeSpan.FromSeconds(timeLeft);
            _waveProgressLabel.Text = time.ToString("mm':'ss");
            _waveProgressBar.Value = totalTime - timeLeft;
        }

        private void InitializeWaveProgressBar(double totalTime)
        {
            _waveProgressBar.MaxValue = totalTime;
            _progressBarInitialized = true;
        }

        private void OnWaveComplete()
        {
            _progressBarInitialized = false;
        }

        public override void _ExitTree()
        {
            EventBus.Instance.WaveStarted -= OnWaveStarted;
            base._ExitTree();
        }
    }
}
