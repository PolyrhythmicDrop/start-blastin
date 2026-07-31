using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Utility;

namespace UI
{
    public partial class WaveComplete : Control
    {
        private bool _introAnimStarted = false;
        private CancellationTokenSource _completeCts;

        // ~~ Label variables ~~ //
        private bool _labelsInitialized = false;
        private bool _labelsAnimating = false;
        private RichTextLabel _waveLabel;
        private Vector2 _waveFinalPos;
        private RichTextLabel _completeLabel;
        private Vector2 _completeFinalPos;
        private Tween _labelTween;

        // ~~~~~~~~~~~~~~~~~~~~~~~//

        // ~~ Line variables ~~ //
        private Line2D _topLine;
        private Vector2 _topLineFinalEndPoint;
        private Sprite2D _topLineEndSprite;
        private Callable _topLineAnimCallable;
        private Line2D _bottomLine;
        private Vector2 _bottomLineFinalEndPoint;
        private Sprite2D _bottomLineEndSprite;
        private Callable _bottomLineAnimCallable;
        private Tween _lineTween;
        private bool _linesAnimating = false;

        // ~~~~~~~~~~ //

        private const float INTRO_ANIM_DUR = 0.25f;

        public override void _Ready()
        {
            _waveLabel = GetNode<RichTextLabel>("%WaveLabel");
            _waveLabel.Hide();
            _completeLabel = GetNode<RichTextLabel>("%CompleteLabel");
            _completeLabel.Hide();

            _topLine = GetNode<Line2D>("%TopLine");
            _topLineEndSprite = GetNode<Sprite2D>("%TopLineEnd");

            _bottomLine = GetNode<Line2D>("%BottomLine");
            _bottomLineEndSprite = GetNode<Sprite2D>("%BottomLineEnd");

            _completeCts = new();

            InitializeLines();
        }

        private void InitializeLabels()
        {
            _labelsInitialized = true;

            // Get the size of each label
            Vector2 waveSize = new(_waveLabel.GetContentWidth(), _waveLabel.GetContentHeight());
            Vector2 completeSize = new(
                _completeLabel.GetContentWidth(),
                _completeLabel.GetContentHeight()
            );

            // Get the size of the viewport
            Vector2 viewSize = GetViewportRect().Size;

            // Set the final position of the wave label.
            float waveFinalY = (viewSize.Y / 2) - (waveSize.Y / 2);
            _waveFinalPos = new(viewSize.X / 20, waveFinalY);

            // Move the wave label offscreen.
            _waveLabel.GlobalPosition = new(_waveFinalPos.X, -waveSize.Y); // <= Vertical

            // Set the final position of the complete label.
            float completeFinalX = (viewSize.X - (viewSize.X / 20)) - completeSize.X;
            float completeFinalY = (viewSize.Y / 2) - (completeSize.Y / 2);
            _completeFinalPos = new(completeFinalX, completeFinalY);

            // Move the complete label offscreen.
            _completeLabel.GlobalPosition = new(completeFinalX, viewSize.Y + completeSize.Y); // <= Vertical

            // Show both labels now that they're moved.
            _waveLabel.Show();
            _completeLabel.Show();
        }

        private void InitializeLines()
        {
            // Clear the points for each line.
            _topLine.ClearPoints();
            _bottomLine.ClearPoints();

            // Set two empty points for both lines.
            // We'll tween the end point during the intro animation.
            _topLine.AddPoint(Vector2.Zero);
            _topLine.AddPoint(Vector2.Zero);
            _bottomLine.AddPoint(Vector2.Zero);
            _bottomLine.AddPoint(Vector2.Zero);

            // Get the size of the viewport
            Vector2 viewSize = GetViewportRect().Size;

            // Set the starting position of the top line to the left size, upper third of the screen.
            float topLineYPos = viewSize.Y / 3;
            _topLine.GlobalPosition = new(0, topLineYPos);
            // Set the final end point of the top line to the other end of the screen.
            _topLineFinalEndPoint = new(viewSize.X + 64, 0);

            // Set the starting position of the bottom line to the right side, lower third of the screen.
            float bottomLineYPos = viewSize.Y - (viewSize.Y / 3);
            _bottomLine.GlobalPosition = new(viewSize.X, bottomLineYPos);
            // Set the final end point of the bottom line to the other end of the screen.
            _bottomLineFinalEndPoint = new(-viewSize.X - 64, 0);

            // Set the Callables
            _topLineAnimCallable = Callable.From(
                (Vector2 endPos) => SetLineEndPoint(endPos, _topLine)
            );
            _bottomLineAnimCallable = Callable.From(
                (Vector2 endPos) => SetLineEndPoint(endPos, _bottomLine)
            );
        }

        private void SetLineEndPoint(Vector2 endPoint, Line2D line)
        {
            int count = line.GetPointCount();
            if (count > 1)
            {
                line.SetPointPosition(count - 1, endPoint);
            }
        }

        public async Task PlayWaveCompleteAnimation()
        {
            CancellationToken token = _completeCts.Token;
            _introAnimStarted = true;

            try
            {
                if (!_labelsInitialized)
                {
                    InitializeLabels();
                }

                await Task.WhenAll(TweenLineIntroAnimation(token), TweenLabelIntroAnimation(token));
                await ToSignal(
                    GetTree().CreateTimer(1.5f, false),
                    SceneTreeTimer.SignalName.Timeout
                );
                await Task.WhenAll(TweenLineOutroAnimation(token), TweenLabelOutroAnimation(token));
            }
            catch (OperationCanceledException e)
            {
                DebugLogger.LogMessage(
                    $"Wave complete animation cancelled! {e.Message}, {e.Source}"
                );
                SkipAnimations();
            }
            finally
            {
                _completeCts.Dispose();
                _completeCts = null;
                _introAnimStarted = false;
                OnAnimationEnd();
            }
        }

        private void SkipAnimations()
        {
            if (_lineTween != null && _lineTween.IsRunning())
            {
                _lineTween.Kill();
            }
            if (_labelTween != null && _labelTween.IsRunning())
            {
                _labelTween.Kill();
            }
        }

        private void OnAnimationEnd()
        {
            if (!IsQueuedForDeletion())
            {
                QueueFree();
            }
        }

        private void AnimateLineEndings()
        {
            int topCount = _topLine.GetPointCount();
            int bottomCount = _bottomLine.GetPointCount();
            // Set the end point sprites to the end point of each line.
            if (topCount > 1)
            {
                _topLineEndSprite.Position = _topLine.GetPointPosition(topCount - 1);
            }
            if (bottomCount > 1)
            {
                _bottomLineEndSprite.Position = _bottomLine.GetPointPosition(bottomCount - 1);
            }
        }

        private async Task TweenLineIntroAnimation(CancellationToken token)
        {
            _linesAnimating = true;

            if (_lineTween != null && _lineTween.IsValid())
            {
                _lineTween.Kill();
            }

            _lineTween = CreateTween();

            _lineTween.TweenMethod(
                _topLineAnimCallable,
                _topLine.GetPointPosition(0),
                _topLineFinalEndPoint,
                INTRO_ANIM_DUR
            );
            _lineTween
                .Parallel()
                .TweenMethod(
                    _bottomLineAnimCallable,
                    _bottomLine.GetPointPosition(0),
                    _bottomLineFinalEndPoint,
                    INTRO_ANIM_DUR
                );

            // await ToSignal(_lineTween, Tween.SignalName.Finished);
            await UtilityMethods.AwaitTweenFinished(_lineTween, token);

            token.ThrowIfCancellationRequested();

            _linesAnimating = false;
        }

        private async Task TweenLineOutroAnimation(CancellationToken token)
        {
            _linesAnimating = true;

            if (_lineTween != null && _lineTween.IsValid())
            {
                _lineTween.Kill();
            }

            Vector2 topFinal = new Vector2(
                _topLine.GetPointPosition(0).X - 64,
                _topLine.GetPointPosition(0).Y
            );
            Vector2 bottomFinal = new Vector2(
                _bottomLine.GetPointPosition(0).X + 64,
                _bottomLine.GetPointPosition(0).Y
            );

            _lineTween = CreateTween();

            _lineTween.TweenMethod(
                _topLineAnimCallable,
                _topLineFinalEndPoint,
                topFinal,
                INTRO_ANIM_DUR
            );
            _lineTween
                .Parallel()
                .TweenMethod(
                    _bottomLineAnimCallable,
                    _bottomLineFinalEndPoint,
                    bottomFinal,
                    INTRO_ANIM_DUR
                );

            // await ToSignal(_lineTween, Tween.SignalName.Finished);
            await UtilityMethods.AwaitTweenFinished(_lineTween, token);

            token.ThrowIfCancellationRequested();

            _linesAnimating = false;
        }

        private async Task TweenLabelIntroAnimation(CancellationToken token)
        {
            _labelsAnimating = true;

            if (_labelTween != null && _labelTween.IsValid())
            {
                _labelTween.Kill();
            }

            _labelTween = CreateTween();

            _labelTween.TweenProperty(_waveLabel, "global_position", _waveFinalPos, INTRO_ANIM_DUR);
            _labelTween
                .Parallel()
                .TweenProperty(
                    _completeLabel,
                    "global_position",
                    _completeFinalPos,
                    INTRO_ANIM_DUR
                );

            await UtilityMethods.AwaitTweenFinished(_labelTween, token);

            token.ThrowIfCancellationRequested();

            _labelsAnimating = false;
        }

        private async Task TweenLabelOutroAnimation(CancellationToken token)
        {
            _labelsAnimating = true;

            if (_labelTween != null && _labelTween.IsValid())
            {
                _labelTween.Kill();
            }

            // Get the size of the viewport
            Vector2 viewSize = GetViewportRect().Size;

            Vector2 waveFinal = new(
                _waveLabel.GlobalPosition.X,
                viewSize.Y + _waveLabel.GetContentHeight()
            );
            Vector2 completeFinal = new(
                _completeLabel.GlobalPosition.X,
                -_completeLabel.GetContentHeight() - 32
            );

            _labelTween = CreateTween();

            _labelTween.TweenProperty(_waveLabel, "global_position", waveFinal, INTRO_ANIM_DUR);
            _labelTween
                .Parallel()
                .TweenProperty(_completeLabel, "global_position", completeFinal, INTRO_ANIM_DUR);

            // await ToSignal(_labelTween, Tween.SignalName.Finished);
            await UtilityMethods.AwaitTweenFinished(_labelTween, token);

            token.ThrowIfCancellationRequested();

            _labelsAnimating = false;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (!_labelsInitialized)
            {
                InitializeLabels();
            }

            if (_linesAnimating)
            {
                AnimateLineEndings();
            }

            if (
                (_labelsAnimating || _linesAnimating)
                && Input.IsActionJustPressed("ui_close_dialog")
            )
            {
                // Cancel the animation.
                _completeCts?.Cancel();
            }
        }
    }
}
