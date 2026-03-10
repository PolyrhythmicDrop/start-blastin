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

        public override void _Ready()
        {
            _label = GetNode<RichTextLabel>("%RichTextLabel");
            Vector2 vSize = GetViewportRect().Size;
            _label.CustomMinimumSize = new(vSize.X * 0.75f, vSize.Y * 0.5f);
        }

        public override void _Process(double delta)
        {
            if (_running)
            {
                _label.QueueRedraw();
            }
        }

        public async Task Start()
        {
            _running = true;

            if (_tween != null && _tween.IsRunning())
            {
                _tween.Kill();
            }

            _tween = CreateTween();

            _tween.TweenInterval(1);
            _tween.TweenProperty(_label, "text", "3", 0);
            _tween.TweenInterval(1);
            _tween.TweenProperty(_label, "text", "2", 0);
            _tween.TweenInterval(1);
            _tween.TweenProperty(_label, "text", "1", 0);
            _tween.TweenInterval(1);
            _tween.TweenProperty(_label, "text", "Start Blastin'!", 0);
            _tween.TweenInterval(1);

            await ToSignal(_tween, Tween.SignalName.Finished);
        }
    }
}
