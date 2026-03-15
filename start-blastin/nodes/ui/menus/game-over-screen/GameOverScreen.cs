using System;
using System.Threading.Tasks;
using Godot;

namespace UI
{
    public partial class GameOverScreen : MenuScreen
    {
        private GameOverSelectionMenu _gameOverMenu;
        private RichTextLabel _gameOverLabel;
        private Tween _gameOverTween;

        private DifficultySelectionMenu _diffSelMenu;

        private Panel _bgBlur;
        private ShaderMaterial _blurShader;

        private const float FINAL_BLUR_MIX = 0.2f;
        private const string SHADER_MIX_PERC = "mix_percentage";

        public override void _Ready()
        {
            _bgBlur = GetNode<Panel>("%UiBlurBG");
            _blurShader = _bgBlur.Material as ShaderMaterial;

            _gameOverMenu = GetNode<GameOverSelectionMenu>("%GameOverSelectionMenu");
            _gameOverMenu.PivotOffsetRatio = new(0.5f, 0.5f);

            _gameOverLabel = GetNode<RichTextLabel>("%GameOverLabel");
            _gameOverLabel.VisibleCharacters = 0;
            _gameOverLabel.PivotOffsetRatio = new(0.5f, 0.5f);

            _diffSelMenu = GetNode<DifficultySelectionMenu>("%DifficultyMenu");

            _initMenu = _gameOverMenu;

            base._Ready();
        }

        protected override async Task Initialize()
        {
            _initialized = true;

            _gameOverMenu.Hide();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            await TweenGameOverTyping();

            await SwitchMenu(_initMenu);
            _selector.Visible = true;
        }

        protected override void AssignMenuActions()
        {
            _gameOverMenu.NewGame.SetSelectAction(async () => await SwitchMenu(_diffSelMenu));
            _gameOverMenu.MainMenu.SetSelectAction(ReturnToMainMenu);
            _gameOverMenu.Quit.SetSelectAction(() =>
            {
                GetTree().Quit();
                return Task.CompletedTask;
            });

            _diffSelMenu.SetDifficultyActions(
                StartNewGame,
                async () => await SwitchMenu(_prevMenu)
            );
        }

        private void SetBlurShaderMod(float mod)
        {
            _blurShader.SetShaderParameter(SHADER_MIX_PERC, Math.Clamp(mod, 0, 1.0));
        }

        private async Task TweenGameOverTyping()
        {
            if (_gameOverTween != null && _gameOverTween.IsValid())
            {
                _gameOverTween.Kill();
            }

            // Set background blur variables
            Callable blurCallable = Callable.From((float mod) => SetBlurShaderMod(mod));

            // Set Game Over text variables
            Vector2 goRectSize = _gameOverLabel.GetGlobalRect().Size;
            Vector2 endPos = _gameOverLabel.GlobalPosition;
            int charCount = _gameOverLabel.GetTotalCharacterCount();
            Vector2 center = GetViewport().GetVisibleRect().Size / 2;

            _gameOverLabel.GlobalPosition = new(endPos.X, center.Y - (goRectSize.Y / 2));

            _gameOverTween = CreateTween();

            _gameOverTween.TweenMethod(blurCallable, 0, FINAL_BLUR_MIX, 0.5f);
            _gameOverTween
                .TweenProperty(_gameOverLabel, "visible_characters", charCount, 1)
                .From(0);
            _gameOverTween
                .TweenProperty(_gameOverLabel, "global_position", endPos, 0.25f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);

            await ToSignal(_gameOverTween, Tween.SignalName.Finished);
        }
    }
}
