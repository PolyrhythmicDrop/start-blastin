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
        private Label _waveProgressLabel;
        private bool _progressBarInitialized;
        private Color _progressColor;

        public override void _Ready()
        {
            _waveCounter = GetNode<Label>("%WaveCounter");
            _waveProgressLabel = GetNode<Label>("%WaveProgressLabel");

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.GameInitialized += OnGameInitialized;
            EventBus.Instance.WaveStarted += OnWaveStarted;
            EventBus.Instance.WaveTimeLeft += OnWaveTimeLeft;
            EventBus.Instance.WaveComplete += OnWaveComplete;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.GameInitialized -= OnGameInitialized;
            EventBus.Instance.WaveStarted -= OnWaveStarted;
            EventBus.Instance.WaveTimeLeft -= OnWaveTimeLeft;
            EventBus.Instance.WaveComplete -= OnWaveComplete;
        }

        public void SetWaveCount(int waveCount)
        {
            _waveCounter.Text = $"Wave {waveCount}";
        }

        private void OnGameInitialized(object sender, GameInitializedEventArgs args)
        {
            SetWaveCount(args.StartingWave);
            SetWaveProgress(args.WaveTime, args.WaveTime);
        }

        private void OnWaveStarted(object sender, WaveStartedEventArgs args) =>
            SetWaveCount(args.Wave);

        private void OnWaveTimeLeft(object sender, WaveTimeLeftEventArgs args) =>
            SetWaveProgress(args.TimeLeft, args.TotalTime);

        private void SetWaveProgress(double timeLeft, double totalTime)
        {
            if (!_progressBarInitialized)
            {
                InitializeWaveProgressBar();
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
        }

        private void InitializeWaveProgressBar()
        {
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
