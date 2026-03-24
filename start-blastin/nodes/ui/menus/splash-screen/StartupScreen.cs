using System;
using System.Threading;
using System.Threading.Tasks;
using Autoloads;
using Godot;
using Utility;
using WaveManagement;

namespace UI
{
    public partial class StartupScreen : MenuScreen
    {
        private StartupSplashSelectionMenu _startupMenu;

        private DifficultySelectionMenu _diffMenu;

        private AnimatedSprite2D _starSprite;
        private Vector2 _starFinalPos;
        private float _starCondenseAnimDur;
        private const float STAR_FINAL_ROTATE_DEG = 180;
        private Godot.Timer _twinkleTimer;
        private const double STAR_TWINKLE_TIME = 4.0f;

        private Control _titleControl;

        private TextureRect _startTexture;
        private Vector2 _startTextFinalPos;
        private TextureRect _blastinTexture;
        private Vector2 _blastinTextFinalPos;

        private ShaderMaterial _shineShader;
        private Godot.Timer _shineTimer;
        private const float SHINE_DELAY_TIME = 6.0f;
        private const string SHINE_PROGRESS_PARAM = "shine_progress";

        private Tween _starTween;
        private Tween _titleTextTween;
        private Tween _shineTween;
        private Tween _outroTween;

        /// <summary>
        /// Cancellation token source for the set of intro Tweens.
        /// </summary>
        private CancellationTokenSource _introCts;

        // State
        private bool _introPlaying;

        public override void _Ready()
        {
            _startupMenu = GetNode<StartupSplashSelectionMenu>("%StartupMenu");
            _initMenu = _startupMenu;

            _diffMenu = GetNode<DifficultySelectionMenu>("%DifficultyMenu");

            _starSprite = GetNode<AnimatedSprite2D>("%Star");
            _starFinalPos = _starSprite.GlobalPosition;

            _starCondenseAnimDur =
                UtilityMethods.GetAnimationDuration(_starSprite, "condense") ?? 2.0f;

            _starSprite.Hide();

            _titleControl = GetNode<Control>("%TitleControl");
            _startTexture = GetNode<TextureRect>("%StarText");
            _startTextFinalPos = _startTexture.GlobalPosition;

            _blastinTexture = GetNode<TextureRect>("%BlastinText");
            _blastinTextFinalPos = _blastinTexture.GlobalPosition;

            _shineShader = _titleControl.Material as ShaderMaterial;

            SetAnimationTimers();

            _titleControl.Hide();

            base._Ready();
        }

        private void SetAnimationTimers()
        {
            _twinkleTimer = new()
            {
                Autostart = false,
                OneShot = true,
                WaitTime = STAR_TWINKLE_TIME,
                Name = "TwinkleTimer",
            };

            _twinkleTimer.Timeout += TwinkleStar;
            AddChild(_twinkleTimer);

            _shineTimer = new()
            {
                Autostart = false,
                OneShot = true,
                WaitTime = SHINE_DELAY_TIME,
                Name = "ShineTimer",
            };

            _shineTimer.Timeout += TweenShine;

            AddChild(_shineTimer);
        }

        protected override void AssignMenuActions()
        {
            // Startup
            _startupMenu.SetEntrySelectAction(
                _startupMenu.NewGame,
                async () =>
                {
                    await SwitchMenu(_diffMenu);
                }
            );
            _startupMenu.SetEntrySelectAction(
                _startupMenu.Quit,
                () =>
                {
                    GetTree().Quit();
                    return Task.CompletedTask;
                }
            );

            // Difficulty
            _diffMenu.SetDifficultyActions(StartNewGame, async () => await SwitchMenu(_prevMenu));
        }

        protected override async Task Initialize()
        {
            _initialized = true;
            _initMenu.Hide();

            _introCts = new CancellationTokenSource();

            try
            {
                await TweenIntro(_introCts.Token);
            }
            catch (OperationCanceledException)
            {
                DebugLogger.LogMessage(
                    $"Skipping intro animation with a cancellation token...",
                    true
                );
                // Skip the intro if any button was pressed.
                SkipIntro();
            }
            finally
            {
                _introCts.Dispose();
                _introCts = null;
            }

            // await OnIntroComplete();
            await OnIntroComplete();
            await SwitchMenu(_initMenu);

            _selector.Visible = true;
        }

        private void SkipIntro()
        {
            if (_starTween != null && _starTween.IsRunning())
            {
                _starTween.EmitSignal(Tween.SignalName.Finished);
                _starTween.Kill();
            }
            if (_titleTextTween != null && _titleTextTween.IsRunning())
            {
                _titleTextTween.EmitSignal(Tween.SignalName.Finished);
                _titleTextTween.Kill();
            }

            _starSprite.GlobalPosition = _starFinalPos;
            _starSprite.RotationDegrees = STAR_FINAL_ROTATE_DEG;
            _starSprite.Scale = Vector2.One;
            _startTexture.GlobalPosition = _startTextFinalPos;
            _blastinTexture.GlobalPosition = _blastinTextFinalPos;
            if (!_titleControl.Visible)
            {
                _titleControl.Show();
            }
        }

        private async Task TweenIntro(CancellationToken token)
        {
            _menuTransitioning = true;
            _introPlaying = true;

            try
            {
                await TweenStarInitialEntrance(token);
                await Task.WhenAll(TweenStarburst(token), TweenTextEntrance(token));
            }
            finally
            {
                _introPlaying = false;
                _menuTransitioning = false;
            }
        }

        private async Task TweenStarInitialEntrance(CancellationToken token)
        {
            if (_starTween != null && _starTween.IsValid())
            {
                _starTween.Kill();
            }

            // Get variables
            Vector2 viewSize = GetViewport().GetVisibleRect().Size;
            Vector2 starCenter = viewSize / 2;

            _starSprite.GlobalPosition = new(starCenter.X, viewSize.Y + 100);
            _starSprite.Scale = new(0.01f, 0.01f);
            _starSprite.Show();

            _starSprite.Play("twinkle");
            _starTween = CreateTween();

            // Tween the twinkle
            _starTween
                .TweenProperty(_starSprite, "scale", Vector2.One, 3f)
                .FromCurrent()
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _starTween
                .Parallel()
                .TweenProperty(_starSprite, "global_position", starCenter, 3f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _starTween
                .Parallel()
                .TweenProperty(_starSprite, "rotation_degrees", STAR_FINAL_ROTATE_DEG * 2, 3);
            _starTween.TweenInterval(1);

            await UtilityMethods.AwaitTweenFinished(_starTween, token);

            // Throw a cancellation exception if we cancel the Task to propagate back up to Initialize().
            token.ThrowIfCancellationRequested();
        }

        private async Task TweenStarburst(CancellationToken token)
        {
            if (_starTween != null && _starTween.IsValid())
            {
                _starTween.Kill();
            }

            _starTween = CreateTween();

            // Tween the condense animation into the final position.
            _starTween.TweenCallback(Callable.From(() => _starSprite.Play("condense")));
            _starTween.TweenProperty(
                _starSprite,
                "global_position",
                _starFinalPos,
                _starCondenseAnimDur
            );
            _starTween
                .Parallel()
                .TweenProperty(
                    _starSprite,
                    "rotation_degrees",
                    STAR_FINAL_ROTATE_DEG * 5,
                    _starCondenseAnimDur
                );

            await UtilityMethods.AwaitTweenFinished(_starTween, token);

            // Throw a cancellation exception if we cancel the Task to propagate back up to Initialize().
            token.ThrowIfCancellationRequested();
        }

        private async Task TweenTextEntrance(CancellationToken token)
        {
            if (_titleTextTween != null && _titleTextTween.IsValid())
            {
                _titleTextTween.Kill();
            }

            // Get variables
            Vector2 viewSize = GetViewport().GetVisibleRect().Size;

            // Initialize title text variables
            _startTexture.GlobalPosition = new(-viewSize.X, _startTextFinalPos.Y);
            _blastinTexture.GlobalPosition = new(-viewSize.X, _blastinTextFinalPos.Y);

            _titleControl.Show();

            // Create the text subtween
            _titleTextTween = CreateTween();

            _titleTextTween.TweenInterval(_starCondenseAnimDur / 2);
            _titleTextTween.TweenProperty(
                _startTexture,
                "global_position",
                _startTextFinalPos,
                _starCondenseAnimDur * 0.25
            );
            _titleTextTween
                .Parallel()
                .TweenProperty(
                    _blastinTexture,
                    "global_position",
                    _blastinTextFinalPos,
                    _starCondenseAnimDur * 0.25
                );

            await UtilityMethods.AwaitTweenFinished(_titleTextTween, token);

            // Throw a cancellation exception if we cancel the Task to propagate back up to Initialize().
            token.ThrowIfCancellationRequested();

            // await ToSignal(_titleTextTween, Tween.SignalName.Finished);
        }

        public override void _Process(double delta)
        {
            if (_introPlaying && Input.IsAnythingPressed())
            {
                // Requests cancellation of the intro.
                // Any callbacks that use a token generated from this CTS are fired automatically.
                _introCts?.Cancel();

                // SkipIntro();
            }
            base._Process(delta);
        }

        private Task OnIntroComplete()
        {
            _starSprite.Play("menu");
            _twinkleTimer.Start();
            _shineTimer.Start();
            return Task.CompletedTask;
        }

        private async void TwinkleStar()
        {
            _starSprite.Play("menu");
            await ToSignal(_starSprite, AnimatedSprite2D.SignalName.AnimationFinished);
            if (_twinkleTimer != null)
            {
                _twinkleTimer?.Start(STAR_TWINKLE_TIME * GD.RandRange(0.5f, 3));
            }
        }

        private async void TweenShine()
        {
            if (_shineTween != null && _shineTween.IsValid())
            {
                _shineTween.Kill();
            }

            Callable shineCallable = Callable.From(
                (float prog) => _shineShader.SetShaderParameter(SHINE_PROGRESS_PARAM, prog)
            );

            _shineTween = CreateTween();

            _shineTween.TweenMethod(shineCallable, 0.0f, 1.0f, 0.5f);

            await ToSignal(_shineTween, Tween.SignalName.Finished);

            if (_shineTimer != null)
            {
                _shineTimer?.Start(SHINE_DELAY_TIME * GD.RandRange(0.5f, 2));
            }
        }

        private async Task TweenOutro()
        {
            // Stop and free the animation timers
            if (_twinkleTimer != null && !_twinkleTimer.IsQueuedForDeletion())
            {
                _twinkleTimer.Stop();
                _twinkleTimer.Timeout -= TwinkleStar;
                _twinkleTimer.QueueFree();
            }

            if (_shineTimer != null && !_shineTimer.IsQueuedForDeletion())
            {
                _shineTimer.Stop();
                _shineTimer.Timeout -= TweenShine;
                _shineTimer.QueueFree();
            }

            // Kill and reset any other animation tweens
            if (_shineTween != null && _shineTween.IsValid())
            {
                _shineTween.Kill();
            }

            if (_outroTween != null && _outroTween.IsValid())
            {
                _outroTween.Kill();
            }

            // Set the variables for the outro
            Vector2 viewSize = GetViewport().GetVisibleRect().Size;

            // Text variables
            float startTextOutroPosX = -_startTexture.Texture.GetWidth();
            Vector2 startTextOutroPos = new(startTextOutroPosX, _startTexture.GlobalPosition.Y);

            float blastinTextOutroPosX = viewSize.X + _blastinTexture.Texture.GetWidth();
            Vector2 blastinTextOutroPos = new(
                blastinTextOutroPosX,
                _blastinTexture.GlobalPosition.Y
            );

            _outroTween = CreateTween();

            _outroTween.TweenProperty(_startTexture, "global_position", startTextOutroPos, 1);
            _outroTween
                .Parallel()
                .TweenProperty(_blastinTexture, "global_position", blastinTextOutroPos, 1);
            _outroTween
                .Parallel()
                .TweenProperty(_starSprite, "scale", Vector2.Zero, 1)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _outroTween
                .Parallel()
                .TweenProperty(_starSprite, "global_rotation_degrees", 1080, 1)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);

            await ToSignal(_outroTween, Tween.SignalName.Finished);
        }

        protected override async Task StartNewGame(Difficulty difficulty)
        {
            _menuTransitioning = true;

            // Wait for the menu transition and the splash screen outro to complete.
            await Task.WhenAll(TweenMenuTransition(_activeMenu, null), TweenOutro());

            SceneManager.Instance.LoadNewGame(difficulty);
        }
    }
}
