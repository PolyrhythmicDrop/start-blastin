using System.Threading.Tasks;
using Autoloads;
using Godot;
using WaveManagement;

namespace UI
{
    public partial class StartupScreen : Node
    {
        // State flags
        private bool _initialized = false;
        private bool _menuTransitioning = false;

        private SelectionMenu _activeMenu;
        private SelectionMenu _prevMenu;
        private AnimatedSprite2D _selector = GD.Load<PackedScene>("uid://deabemykbs2vl")
            .Instantiate<AnimatedSprite2D>();

        // Tween stuff
        private Tween _selTween;

        private ColorPalette _alphaPalette = ResourceLoader.Load<ColorPalette>(
            "uid://jo6hbayjhfba"
        );

        private const float MENU_TRANS_DUR = 0.2f;

        private StartupSplashSelectionMenu _startupMenu;

        private DifficultySelectionMenu _diffMenu;

        public override void _Ready()
        {
            _startupMenu = GetNode<StartupSplashSelectionMenu>("%StartupMenu");

            _diffMenu = GetNode<DifficultySelectionMenu>("%DifficultyMenu");

            AddChild(_selector);
            AssignMenuActions();
        }

        private void AssignMenuActions()
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
            _diffMenu.SetEntrySelectAction(
                _diffMenu.Easy,
                async () => await StartNewGame(Difficulty.Easy)
            );
            _diffMenu.SetEntrySelectAction(
                _diffMenu.Medium,
                async () => await StartNewGame(Difficulty.Medium)
            );
            _diffMenu.SetEntrySelectAction(
                _diffMenu.Hard,
                async () => await StartNewGame(Difficulty.Hard)
            );
            _diffMenu.SetEntrySelectAction(
                _diffMenu.Back,
                async () => await SwitchMenu(_startupMenu)
            );
        }

        private async Task Initialize()
        {
            _initialized = true;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            await SwitchMenu(_startupMenu);
            _selector.Visible = true;
        }

        private async Task TweenSelectorIn(Vector2 finalPos)
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = new Vector2(finalPos.X - 300, finalPos.Y);
            Color full = _alphaPalette.Colors[0];
            Color trans = _alphaPalette.Colors[1];

            _selTween = _selector
                .CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween.TweenProperty(_selector, "modulate", full, MENU_TRANS_DUR).From(trans);
            _selTween
                .TweenProperty(_selector, "global_position", finalPos, MENU_TRANS_DUR)
                .From(startPos);
            _selTween.Chain().TweenCallback(Callable.From(() => _selector.Play("default")));

            await ToSignal(_selTween, Tween.SignalName.Finished);
        }

        private void TweenSelectorOut()
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = _selector.GlobalPosition;
            Vector2 endPos = new Vector2(startPos.X - 300, startPos.Y);

            Color full = _alphaPalette.Colors[0];
            Color trans = _alphaPalette.Colors[1];

            _selector.Play("spin");
            _selTween = _selector
                .CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween.TweenProperty(_selector, "modulate", trans, MENU_TRANS_DUR).From(full);
            _selTween.TweenProperty(_selector, "global_position", endPos, MENU_TRANS_DUR);
        }

        private void TweenSelectorIdle()
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = _selector.GlobalPosition;
            Vector2 endPos = new(startPos.X - 10, startPos.Y);

            _selTween = _selector
                .CreateTween()
                .SetLoops()
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween.TweenProperty(_selector, "global_position", endPos, 1f);
            _selTween.TweenProperty(_selector, "global_position", startPos, 1f);
        }

        private async Task SwitchMenu(SelectionMenu menu)
        {
            _menuTransitioning = true;

            if (_activeMenu != null)
            {
                _prevMenu = _activeMenu;
            }

            _activeMenu = menu;

            if (!_activeMenu.RememberEntry)
            {
                _activeMenu.CurrentEntryIndex = 0;
            }

            await TweenMenuTransition(_prevMenu, _activeMenu);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            // _activeMenu.CurrentEntryIndex = 0;
            // await TweenSelectorIn(GetEntrySelectorPoint(_currentEntryIndex));
            await TweenSelectorIn(_activeMenu.CurrentEntry.GetEntrySelectorPoint());

            // TweenSelectorIdle();

            _menuTransitioning = false;
        }

        private async Task TweenMenuTransition(Control prevMenu, Control nextMenu)
        {
            Color full = _alphaPalette.Colors[0];
            Color trans = _alphaPalette.Colors[1];

            Tween t = CreateTween();
            if (prevMenu != null)
            {
                t.SetParallel(true);
                t.TweenProperty(prevMenu, "modulate", trans, MENU_TRANS_DUR).From(full);
                t.TweenProperty(prevMenu, "scale", new Vector2(3f, 0.5f), MENU_TRANS_DUR)
                    .From(Vector2.One);
                t.TweenCallback(Callable.From(TweenSelectorOut));
                t.SetParallel(false);
                t.Chain().TweenCallback(Callable.From(prevMenu.Hide));
            }
            if (nextMenu != null)
            {
                t.TweenCallback(Callable.From(nextMenu.Show));
                t.SetParallel(true);
                t.TweenProperty(nextMenu, "scale", Vector2.One, MENU_TRANS_DUR)
                    .From(new Vector2(3f, 0.5f));
                t.TweenProperty(nextMenu, "modulate", full, MENU_TRANS_DUR).From(trans);
            }

            await ToSignal(t, Tween.SignalName.Finished);
        }

        public override void _Process(double delta)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (!_menuTransitioning)
            {
                if (Input.IsActionJustPressed("ui_down") || Input.IsActionJustPressed("ui_right"))
                {
                    _activeMenu.IncrementCurrentIndex(_selector);
                }
                else if (Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_left"))
                {
                    // DecrementCurrentIndex();
                    _activeMenu.DecrementCurrentIndex(_selector);
                }
                else if (Input.IsActionJustPressed("ui_accept"))
                {
                    // MakeSelection();
                    _activeMenu.MakeSelection();
                }
                else if (Input.IsActionJustPressed("ui_cancel"))
                {
                    if (_activeMenu.Equals(_startupMenu))
                    {
                        return;
                    }
                    else
                    {
                        SwitchMenu(_prevMenu);
                    }
                }
            }
        }

        private async Task StartNewGame(Difficulty difficulty)
        {
            _menuTransitioning = true;
            await TweenMenuTransition(_activeMenu, null);
            SceneManager.Instance.LoadNewGame(difficulty);
        }
    }
}
