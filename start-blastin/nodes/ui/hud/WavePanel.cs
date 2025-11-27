using System;
using Autoloads;
using Events;
using Godot;
using Interfaces;

namespace UI.HUD
{
    [GlobalClass]
    public partial class WavePanel : PanelContainer, IListener
    {
        private Label _waveCounter;
        private PanelContainer _waveProgressContainer;

        // private Control _waveProgressControl;
        // private TextureProgressBar _waveProgressBar;
        private Label _waveProgressLabel;
        private bool _progressBarInitialized;
        private Color _progressColor;

        public override void _Ready()
        {
            _waveCounter = GetNode<Label>("%WaveCounter");
            // _waveProgressControl = GetNode<Control>("%WaveProgressControl");
            // _waveProgressBar = GetNode<TextureProgressBar>("%WaveProgressBar");
            _waveProgressLabel = GetNode<Label>("%WaveProgressLabel");

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.WaveStarted += OnWaveStarted;
            EventBus.Instance.WaveTimeLeft += OnWaveTimeLeft;
            EventBus.Instance.WaveComplete += OnWaveComplete;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.WaveStarted -= OnWaveStarted;
            EventBus.Instance.WaveTimeLeft -= OnWaveTimeLeft;
            EventBus.Instance.WaveComplete -= OnWaveComplete;
        }

        private void OnWaveStarted(object sender, WaveStartedEventArgs args) =>
            SetWaveCount(args.Wave);

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

            if (timeLeft <= 6)
            {
                Color color = new(Colors.Coral);
                if (_progressColor != color)
                {
                    _progressColor = color;
                    _waveProgressLabel.LabelSettings.FontColor = _progressColor;
                }
            }

            // _waveProgressBar.Value = totalTime - timeLeft;
        }

        private void InitializeWaveProgressBar(double totalTime)
        {
            // _waveProgressBar.MaxValue = totalTime;
            _progressBarInitialized = true;
            _progressColor = new(Colors.MediumAquamarine);
            _waveProgressLabel.LabelSettings.FontColor = _progressColor;
        }

        private void OnWaveComplete()
        {
            _progressBarInitialized = false;
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
