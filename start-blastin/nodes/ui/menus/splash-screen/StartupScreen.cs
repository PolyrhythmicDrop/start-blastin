using System;
using System.Threading.Tasks;
using Autoloads;
using Godot;
using Utility;

namespace UI
{
    public partial class StartupScreen : MenuScreen
    {
        private StartupSplashSelectionMenu _startupMenu;

        private DifficultySelectionMenu _diffMenu;

        private AnimatedSprite2D _starSprite;
        private Vector2 _starFinalPos;
        private const float STAR_FINAL_ROTATE_DEG = 180;
        private Timer _twinkleTimer;
        private const double STAR_TWINKLE_TIME = 4.0f;

        private Control _titleControl;

        private TextureRect _startTexture;
        private Vector2 _startTextFinalPos;
        private TextureRect _blastinTexture;
        private Vector2 _blastinTextFinalPos;

        private ShaderMaterial _shineShader;
        private Timer _shineTimer;
        private const float SHINE_DELAY_TIME = 6.0f;
        private const string SHINE_PROGRESS_PARAM = "shine_progress";

        private Tween _splashIntroTween;
        private Tween _shineTween;

        public override void _Ready()
        {
            _startupMenu = GetNode<StartupSplashSelectionMenu>("%StartupMenu");
            _initMenu = _startupMenu;

            _diffMenu = GetNode<DifficultySelectionMenu>("%DifficultyMenu");

            _starSprite = GetNode<AnimatedSprite2D>("%Star");
            _starFinalPos = _starSprite.GlobalPosition;
            _twinkleTimer = new()
            {
                Autostart = false,
                OneShot = true,
                WaitTime = STAR_TWINKLE_TIME,
                Name = "TwinkleTimer",
            };

            _twinkleTimer.Timeout += async () =>
            {
                await TwinkleStar();
                _twinkleTimer.Start(STAR_TWINKLE_TIME * GD.RandRange(0.5f, 3));
            };
            AddChild(_twinkleTimer);

            _shineTimer = new()
            {
                Autostart = false,
                OneShot = true,
                WaitTime = SHINE_DELAY_TIME,
                Name = "ShineTimer",
            };

            _shineTimer.Timeout += async () =>
            {
                await TweenShine();
                _shineTimer.Start(SHINE_DELAY_TIME * GD.RandRange(0.5f, 2));
            };

            AddChild(_shineTimer);

            _starSprite.Hide();

            _titleControl = GetNode<Control>("%TitleControl");
            _startTexture = GetNode<TextureRect>("%StarText");
            _startTextFinalPos = _startTexture.GlobalPosition;

            _blastinTexture = GetNode<TextureRect>("%BlastinText");
            _blastinTextFinalPos = _blastinTexture.GlobalPosition;

            _shineShader = _titleControl.Material as ShaderMaterial;

            _titleControl.Hide();

            base._Ready();
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

            await TweenSplashScreenIntro();
            await SwitchMenu(_initMenu);

            _selector.Visible = true;
        }

        private async Task TweenSplashScreenIntro()
        {
            // Play the animation and tween rotation and position.
            if (_splashIntroTween != null && _splashIntroTween.IsValid())
            {
                _splashIntroTween.Kill();
            }

            // Get variables
            Vector2 viewSize = GetViewport().GetVisibleRect().Size;
            Vector2 starCenter = viewSize / 2;

            _starSprite.GlobalPosition = new(starCenter.X, viewSize.Y + 100);
            _starSprite.Scale = new(0.01f, 0.01f);
            _starSprite.Show();

            // Initialize title text variables
            float startTextStartPosX = -_startTexture.Texture.GetWidth();
            _startTexture.GlobalPosition = new(startTextStartPosX, _startTextFinalPos.Y);

            float blastinTextStartPosX = viewSize.X + _blastinTexture.Texture.GetWidth();
            _blastinTexture.GlobalPosition = new(blastinTextStartPosX, _blastinTextFinalPos.Y);

            _titleControl.Show();

            float condenseAnimDur =
                UtilityMethods.GetAnimationDuration(_starSprite, "condense") ?? 2.0f;

            // Create the text subtween
            Tween textSubtween = CreateTween();
            textSubtween.TweenInterval(condenseAnimDur / 2);
            textSubtween.TweenProperty(
                _startTexture,
                "global_position",
                _startTextFinalPos,
                condenseAnimDur / 2
            );
            textSubtween
                .Parallel()
                .TweenProperty(
                    _blastinTexture,
                    "global_position",
                    _blastinTextFinalPos,
                    condenseAnimDur / 2
                );

            _starSprite.Play("twinkle");
            _splashIntroTween = CreateTween();

            // Tween the twinkle
            _splashIntroTween
                .TweenProperty(_starSprite, "scale", Vector2.One, 3f)
                .FromCurrent()
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _splashIntroTween
                .Parallel()
                .TweenProperty(_starSprite, "global_position", starCenter, 3f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _splashIntroTween
                .Parallel()
                .TweenProperty(_starSprite, "rotation_degrees", STAR_FINAL_ROTATE_DEG * 2, 3);

            // Tween the condense animation into the final position.
            _splashIntroTween.TweenInterval(1);
            _splashIntroTween.TweenCallback(Callable.From(() => _starSprite.Play("condense")));
            _splashIntroTween.TweenProperty(
                _starSprite,
                "global_position",
                _starFinalPos,
                condenseAnimDur
            );
            // Tween in the text
            _splashIntroTween.Parallel().TweenSubtween(textSubtween);
            _splashIntroTween
                .Parallel()
                .TweenProperty(
                    _starSprite,
                    "rotation_degrees",
                    STAR_FINAL_ROTATE_DEG * 5,
                    condenseAnimDur
                );

            await ToSignal(_splashIntroTween, Tween.SignalName.Finished);

            _starSprite.Play("menu");
            _twinkleTimer.Start();
            _shineTimer.Start();
        }

        private async Task TwinkleStar()
        {
            _starSprite.Play("menu");
            await ToSignal(_starSprite, AnimatedSprite2D.SignalName.AnimationFinished);
        }

        private async Task TweenShine()
        {
            if (_shineTween != null && _shineTween.IsValid())
            {
                _shineTween.Kill();
            }

            Callable shineCallable = Callable.From(
                (float prog) => _shineShader.SetShaderParameter(SHINE_PROGRESS_PARAM, prog)
            );

            _shineTween = CreateTween();

            _shineTween
                .TweenMethod(shineCallable, 0.0f, 1.0f, 1.5f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);

            await ToSignal(_shineTween, Tween.SignalName.Finished);
        }
    }
}
