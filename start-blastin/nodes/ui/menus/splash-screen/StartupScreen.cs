using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Autoloads;
using Godot;
using Utility;
using WaveManagement;

namespace UI
{
    public partial class StartupScreen : Node
    {
        // State flags
        private bool _initialized = false;
        private bool _menuTransitioning = false;

        private Control _activeMenu;
        private Control _prevMenu;
        private AnimatedSprite2D _selector;

        // Tween stuff
        private Tween _selTween;
        private Color _fullColor = new Color(1, 1, 1, 1);
        private Color _transColor = new Color(1, 1, 1, 0);

        private const float MENU_TRANS_DUR = 0.2f;

        private VBoxContainer _initVBox;
        private RichTextLabel _newGameLabel;
        private RichTextLabel _optionsLabel;
        private RichTextLabel _quit;

        private VBoxContainer _diffVBox;
        private RichTextLabel _easy;
        private RichTextLabel _medium;
        private RichTextLabel _hard;
        private RichTextLabel _diffBack;

        private List<Control> _entryList = new();

        private int _currentEntryIndex;
        public int CurrentEntryIndex
        {
            get => _currentEntryIndex;
            set { _currentEntryIndex = value; }
        }

        public override void _Ready()
        {
            _selector = GetNode<AnimatedSprite2D>("%Selector");

            _initVBox = GetNode<VBoxContainer>("%InitVBox");
            _newGameLabel = GetNode<RichTextLabel>("%NewGame");
            _optionsLabel = GetNode<RichTextLabel>("%Options");
            _quit = GetNode<RichTextLabel>("%Quit");

            _diffVBox = GetNode<VBoxContainer>("%DifficultyVBox");
            _easy = GetNode<RichTextLabel>("%Easy");
            _medium = GetNode<RichTextLabel>("%Medium");
            _hard = GetNode<RichTextLabel>("%Hard");
            _diffBack = GetNode<RichTextLabel>("%Back");
        }

        private async Task Initialize()
        {
            _initialized = true;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            await SwitchMenu(_initVBox);
            _selector.Visible = true;
        }

        private void MoveSelectorToEntry(int index)
        {
            if (_entryList[index] == null)
            {
                return;
            }

            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            _selector.GlobalPosition = GetEntrySelectorPoint(index);
            TweenSelectorIdle();
        }

        private Vector2 GetEntrySelectorPoint(int index)
        {
            Control entry = _entryList[index];
            Vector2 cSize = entry.Size;
            float yPos = entry.GlobalPosition.Y + (cSize.Y / 2);
            return new Vector2(entry.GlobalPosition.X, yPos);
        }

        private async Task TweenSelectorIn(Vector2 finalPos)
        {
            if (_selTween != null && _selTween.IsValid())
            {
                _selTween.Kill();
            }

            Vector2 startPos = new Vector2(finalPos.X - 300, finalPos.Y);

            _selTween = _selector
                .CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween
                .TweenProperty(_selector, "modulate", _fullColor, MENU_TRANS_DUR)
                .From(_transColor);
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

            _selector.Play("spin");
            _selTween = _selector
                .CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _selTween
                .TweenProperty(_selector, "modulate", _transColor, MENU_TRANS_DUR)
                .From(_fullColor);
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

        private async Task SwitchMenu(Control menu)
        {
            _menuTransitioning = true;

            if (_activeMenu != null)
            {
                _prevMenu = _activeMenu;
            }

            _activeMenu = menu;

            _entryList?.Clear();
            var mc = menu.GetChildren();
            foreach (Control c in mc)
            {
                _entryList.Add(c);
            }

            await TweenMenuTransition(_prevMenu, _activeMenu);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CurrentEntryIndex = 0;
            await TweenSelectorIn(GetEntrySelectorPoint(_currentEntryIndex));

            TweenSelectorIdle();

            _menuTransitioning = false;
        }

        private async Task TweenMenuTransition(Control prevMenu, Control nextMenu)
        {
            Tween t = CreateTween();
            if (prevMenu != null)
            {
                t.SetParallel(true);
                t.TweenProperty(prevMenu, "modulate", _transColor, MENU_TRANS_DUR).From(_fullColor);
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
                t.TweenProperty(nextMenu, "modulate", _fullColor, MENU_TRANS_DUR).From(_transColor);
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
                    IncrementCurrentIndex();
                }
                else if (Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_left"))
                {
                    DecrementCurrentIndex();
                }
                else if (Input.IsActionJustPressed("ui_accept"))
                {
                    MakeSelection();
                }
                else if (Input.IsActionJustPressed("ui_cancel"))
                {
                    if (_activeMenu.Equals(_initVBox))
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

        private void MakeSelection()
        {
            switch (_entryList[_currentEntryIndex])
            {
                case var entry when entry == _newGameLabel:
                    SwitchMenu(_diffVBox);
                    break;
                case var entry when entry == _quit:
                    GetTree().Quit();
                    break;
                case var entry when entry == _easy:
                    StartNewGame(Difficulty.Easy);
                    break;
                case var entry when entry == _medium:
                    StartNewGame(Difficulty.Medium);
                    break;
                case var entry when entry == _hard:
                    StartNewGame(Difficulty.Hard);
                    break;
                case var entry when entry == _diffBack:
                    SwitchMenu(_initVBox);
                    break;
            }
        }

        private async Task StartNewGame(Difficulty difficulty)
        {
            _menuTransitioning = true;
            await TweenMenuTransition(_activeMenu, null);
            SceneManager.Instance.LoadNewGame(difficulty);
        }

        private void IncrementCurrentIndex()
        {
            int count = _entryList.Count;
            if (count - 1 > _currentEntryIndex)
            {
                CurrentEntryIndex += 1;
            }
            else
            {
                CurrentEntryIndex = 0;
            }
            MoveSelectorToEntry(CurrentEntryIndex);
        }

        private void DecrementCurrentIndex()
        {
            int count = _entryList.Count;
            if (_currentEntryIndex != 0)
            {
                CurrentEntryIndex -= 1;
            }
            else
            {
                CurrentEntryIndex = count - 1;
            }
            MoveSelectorToEntry(CurrentEntryIndex);
        }
    }
}
