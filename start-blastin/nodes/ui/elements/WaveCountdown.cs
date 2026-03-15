using System.Threading.Tasks;
using Godot;

namespace UI
{
    [GlobalClass]
    public partial class WaveCountdown : CenterContainer
    {
        private bool _running = false;

        private RichTextLabel _label;

        private Tween _tween;

        private const string START_BLASTIN =
            "[shake rate=30.0 level=10 connected=1]Start Blastin'![/shake]";

        private const string TEXT_PROP = "text";

        private const float NUM_PREFADE_DUR = 0.60f;
        private const float NUM_FADE_DUR = 0.20f;
        private const float NUM_INTERVAL_DUR = 0.20f;

        public override void _Ready()
        {
            _label = GetNode<RichTextLabel>("%RichTextLabel");
            _label.PivotOffsetRatio = new(0.5f, 0.5f);
            Vector2 vSize = GetViewportRect().Size;
            _label.CustomMinimumSize = new(vSize.X * 0.75f, vSize.Y * 0.5f);
        }

        public override void _Process(double delta)
        {
            // if (_running)
            // {
            //     _label.QueueRedraw();
            // }
        }

        public async Task Start()
        {
            _running = true;

            if (_tween != null && _tween.IsRunning())
            {
                _tween.Kill();
            }

            Color trans = new(1, 1, 1, 0);
            Color opaque = new(1, 1, 1, 1);

            Tween expand = CreateTween();
            expand.TweenProperty(_label, "scale", new Vector2(3, 3), NUM_FADE_DUR);
            expand.Parallel().TweenProperty(_label, "modulate", trans, NUM_FADE_DUR);

            Tween revertSize = CreateTween();
            revertSize.TweenProperty(_label, "scale", new Vector2(1, 1), 0);
            revertSize.Parallel().TweenProperty(_label, "modulate", opaque, 0);

            _tween = CreateTween();

            _tween.TweenInterval(1);
            _tween.TweenProperty(_label, TEXT_PROP, "3", 0);
            _tween.TweenInterval(NUM_PREFADE_DUR);
            _tween.TweenSubtween(expand);
            _tween.TweenInterval(NUM_INTERVAL_DUR);

            _tween.TweenProperty(_label, TEXT_PROP, "2", 0);
            _tween.TweenSubtween(revertSize);
            _tween.TweenInterval(NUM_PREFADE_DUR);
            _tween.TweenSubtween(expand);
            _tween.TweenInterval(NUM_INTERVAL_DUR);

            _tween.TweenProperty(_label, TEXT_PROP, "1", 0);
            _tween.TweenSubtween(revertSize);
            _tween.TweenInterval(NUM_PREFADE_DUR);
            _tween.TweenSubtween(expand);
            _tween.TweenInterval(NUM_INTERVAL_DUR);

            _tween.TweenProperty(_label, TEXT_PROP, START_BLASTIN, 0);
            _tween.TweenProperty(_label, "scale", Vector2.One, NUM_FADE_DUR);
            // _tween.TweenSubtween(revertSize);
            _tween
                .Parallel()
                .TweenProperty(_label, "modulate", new Color("#8bfcee"), NUM_FADE_DUR);

            _tween.Chain().TweenInterval(1.5f);
            _tween.TweenProperty(_label, "modulate", trans, 0.5f);
            _tween.TweenInterval(0.5f);

            await ToSignal(_tween, Tween.SignalName.Finished);
        }
    }
}
