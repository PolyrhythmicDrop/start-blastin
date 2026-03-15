using System.Threading.Tasks;
using Autoloads;
using Godot;
using WaveManagement;

namespace UI
{
    public partial class MenuScreen : Node
    {
        protected bool _initialized = false;
        protected bool _menuTransitioning = false;

        protected SelectionMenu _initMenu;
        protected SelectionMenu _activeMenu;
        protected SelectionMenu _prevMenu;
        protected MenuSelector _selector = GD.Load<PackedScene>("uid://deabemykbs2vl")
            .Instantiate<MenuSelector>();

        protected ColorPalette _alphaPalette = ResourceLoader.Load<ColorPalette>(
            "uid://jo6hbayjhfba"
        );

        protected const float MENU_TRANS_DUR = 0.15f;

        protected virtual async Task Initialize()
        {
            _initialized = true;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            await SwitchMenu(_initMenu);
            _selector.Visible = true;
        }

        public override void _Ready()
        {
            AddChild(_selector);
            AssignMenuActions();
        }

        /// <summary>
        /// Override this method to set callbacks for each menu entry.
        /// </summary>
        protected virtual void AssignMenuActions() { }

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
                    ChangeCurrentEntry(true);
                }
                else if (Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_left"))
                {
                    ChangeCurrentEntry(false);
                }
                else if (Input.IsActionJustPressed("ui_accept"))
                {
                    _activeMenu.MakeSelection();
                }
                else if (Input.IsActionJustPressed("ui_cancel"))
                {
                    if (_activeMenu.Equals(_initMenu))
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

        /// <summary>
        /// Changes the current entry in the active menu.
        /// Moves the selector and starts the selector idle animation.
        /// </summary>
        /// <param name="next">True to increment the active menu's entry index (go to the "next" index), false to decrement it.</param>
        protected void ChangeCurrentEntry(bool next)
        {
            if (next)
                _activeMenu.IncrementCurrentIndex(_selector);
            else
                _activeMenu.DecrementCurrentIndex(_selector);

            _selector.MoveSelectorToEntry(_activeMenu.CurrentEntry);
            _selector.TweenSelectorIdle();
        }

        protected async Task SwitchMenu(SelectionMenu menu)
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
            await _selector.TweenSelectorIn(_activeMenu.CurrentEntry.GetEntrySelectorPoint());

            _selector.TweenSelectorIdle();

            _menuTransitioning = false;
        }

        protected virtual async Task TweenMenuTransition(Control prevMenu, Control nextMenu)
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
                t.TweenCallback(Callable.From(_selector.TweenSelectorOut));
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

        // Common actions

        protected async Task StartNewGame(Difficulty difficulty)
        {
            _menuTransitioning = true;
            await TweenMenuTransition(_activeMenu, null);
            SceneManager.Instance.LoadNewGame(difficulty);
        }

        protected async Task ReturnToMainMenu()
        {
            _menuTransitioning = true;
            await TweenMenuTransition(_activeMenu, null);
            SceneManager.Instance.LoadMainMenu();
        }
    }
}
