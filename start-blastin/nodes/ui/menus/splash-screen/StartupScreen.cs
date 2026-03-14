using System.Threading.Tasks;
using Autoloads;
using Godot;
using WaveManagement;

namespace UI
{
    public partial class StartupScreen : MenuScreen
    {
        private StartupSplashSelectionMenu _startupMenu;

        private DifficultySelectionMenu _diffMenu;

        public override void _Ready()
        {
            _startupMenu = GetNode<StartupSplashSelectionMenu>("%StartupMenu");
            _initMenu = _startupMenu;

            _diffMenu = GetNode<DifficultySelectionMenu>("%DifficultyMenu");

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

        // private async Task SwitchMenu(SelectionMenu menu)
        // {
        //     _menuTransitioning = true;

        //     if (_activeMenu != null)
        //     {
        //         _prevMenu = _activeMenu;
        //     }

        //     _activeMenu = menu;

        //     if (!_activeMenu.RememberEntry)
        //     {
        //         _activeMenu.CurrentEntryIndex = 0;
        //     }

        //     await TweenMenuTransition(_prevMenu, _activeMenu);
        //     await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        //     await _selector.TweenSelectorIn(_activeMenu.CurrentEntry.GetEntrySelectorPoint());

        //     _selector.TweenSelectorIdle();

        //     _menuTransitioning = false;
        // }

        // private async Task TweenMenuTransition(Control prevMenu, Control nextMenu)
        // {
        //     Color full = _alphaPalette.Colors[0];
        //     Color trans = _alphaPalette.Colors[1];

        //     Tween t = CreateTween();
        //     if (prevMenu != null)
        //     {
        //         t.SetParallel(true);
        //         t.TweenProperty(prevMenu, "modulate", trans, MENU_TRANS_DUR).From(full);
        //         t.TweenProperty(prevMenu, "scale", new Vector2(3f, 0.5f), MENU_TRANS_DUR)
        //             .From(Vector2.One);
        //         t.TweenCallback(Callable.From(_selector.TweenSelectorOut));
        //         t.SetParallel(false);
        //         t.Chain().TweenCallback(Callable.From(prevMenu.Hide));
        //     }
        //     if (nextMenu != null)
        //     {
        //         t.TweenCallback(Callable.From(nextMenu.Show));
        //         t.SetParallel(true);
        //         t.TweenProperty(nextMenu, "scale", Vector2.One, MENU_TRANS_DUR)
        //             .From(new Vector2(3f, 0.5f));
        //         t.TweenProperty(nextMenu, "modulate", full, MENU_TRANS_DUR).From(trans);
        //     }

        //     await ToSignal(t, Tween.SignalName.Finished);
        // }

        // /// <summary>
        // /// Changes the current entry in the active menu.
        // /// Moves the selector and starts the selector idle animation.
        // /// </summary>
        // /// <param name="next">True to increment the active menu's entry index (go to the "next" index), false to decrement it.</param>
        // private void ChangeCurrentEntry(bool next)
        // {
        //     if (next)
        //         _activeMenu.IncrementCurrentIndex(_selector);
        //     else
        //         _activeMenu.DecrementCurrentIndex(_selector);

        //     _selector.MoveSelectorToEntry(_activeMenu.CurrentEntry);
        //     _selector.TweenSelectorIdle();
        // }

        // public override void _Process(double delta)
        // {
        //     if (!_initialized)
        //     {
        //         Initialize();
        //     }

        //     if (!_menuTransitioning)
        //     {
        //         if (Input.IsActionJustPressed("ui_down") || Input.IsActionJustPressed("ui_right"))
        //         {
        //             ChangeCurrentEntry(true);
        //         }
        //         else if (Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_left"))
        //         {
        //             ChangeCurrentEntry(false);
        //         }
        //         else if (Input.IsActionJustPressed("ui_accept"))
        //         {
        //             _activeMenu.MakeSelection();
        //         }
        //         else if (Input.IsActionJustPressed("ui_cancel"))
        //         {
        //             if (_activeMenu.Equals(_startupMenu))
        //             {
        //                 return;
        //             }
        //             else
        //             {
        //                 SwitchMenu(_prevMenu);
        //             }
        //         }
        //     }
        // }

        private async Task StartNewGame(Difficulty difficulty)
        {
            _menuTransitioning = true;
            await TweenMenuTransition(_activeMenu, null);
            SceneManager.Instance.LoadNewGame(difficulty);
        }
    }
}
