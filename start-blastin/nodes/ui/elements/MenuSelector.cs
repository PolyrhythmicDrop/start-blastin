using System;
using System.Threading.Tasks;
using Godot;

namespace UI
{
    public partial class MenuSelector : AnimatedSprite2D
    {
        private Tween _selTween;

        private ColorPalette _alphaPalette = ResourceLoader.Load<ColorPalette>(
            "uid://jo6hbayjhfba"
        );

        private const float TRANS_DUR = 0.2f;

        public override void _Ready()
        {
            base._Ready();
            TweenSelectorIdle();
        }

        public async Task TweenSelectorIn(Vector2 finalPos)
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = new Vector2(finalPos.X - 300, finalPos.Y);
            Color full = _alphaPalette.Colors[0];
            Color trans = _alphaPalette.Colors[1];

            _selTween = CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween.TweenProperty(this, "modulate", full, TRANS_DUR).From(trans);
            _selTween.TweenProperty(this, "global_position", finalPos, TRANS_DUR).From(startPos);
            _selTween.Chain().TweenCallback(Callable.From(() => Play("default")));

            await ToSignal(_selTween, Tween.SignalName.Finished);
        }

        public void TweenSelectorOut()
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = GlobalPosition;
            Vector2 endPos = new Vector2(startPos.X - 300, startPos.Y);

            Color full = _alphaPalette.Colors[0];
            Color trans = _alphaPalette.Colors[1];

            Play("spin");
            _selTween = CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween.TweenProperty(this, "modulate", trans, TRANS_DUR).From(full);
            _selTween.TweenProperty(this, "global_position", endPos, TRANS_DUR);
        }

        public void TweenSelectorIdle()
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = GlobalPosition;
            Vector2 endPos = new(startPos.X - 10, startPos.Y);

            _selTween = CreateTween()
                .SetLoops()
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween.TweenProperty(this, "global_position", endPos, 1f);
            _selTween.TweenProperty(this, "global_position", startPos, 1f);
        }

        public void MoveSelectorToEntry(SelectionEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            // if (_selTween != null && _selTween.IsValid())
            // {
            //     _selTween.Kill();
            // }

            GlobalPosition = entry.GetEntrySelectorPoint();
            // TweenSelectorIdle();
        }
    }
}
