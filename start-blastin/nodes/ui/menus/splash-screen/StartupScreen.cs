using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Autoloads;
using Godot;

namespace UI
{
    public partial class StartupScreen : Node
    {
        private bool _initialized = false;
        private Control _activeMenu;
        private Control _prevMenu;
        private AnimatedSprite2D _selector;

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
            set
            {
                _currentEntryIndex = value;
                MoveSelector(value);
            }
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

            await SwitchMenu(_initVBox);
            _selector.Visible = true;
        }

        private void MoveSelector(int index)
        {
            if (_entryList[index] == null)
            {
                return;
            }

            Rect2 cRect = _entryList[index].GetGlobalRect();
            float yPos = cRect.Position.Y + (cRect.Size.Y / 2);
            _selector.Position = new Vector2(cRect.Position.X, yPos);
        }

        private async Task SwitchMenu(Control menu)
        {
            if (_activeMenu != null)
            {
                _activeMenu.Hide();
                _prevMenu = _activeMenu;
            }

            _activeMenu = menu;
            _activeMenu.Show();

            _entryList?.Clear();
            var mc = menu.GetChildren();
            foreach (Control c in mc)
            {
                _entryList.Add(c);
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            CurrentEntryIndex = 0;
        }

        public override void _Process(double delta)
        {
            if (!_initialized)
            {
                Initialize();
            }

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
                    SceneManager.Instance.LoadNewGame(WaveManagement.Difficulty.Easy);
                    break;
                case var entry when entry == _medium:
                    SceneManager.Instance.LoadNewGame(WaveManagement.Difficulty.Medium);
                    break;
                case var entry when entry == _hard:
                    SceneManager.Instance.LoadNewGame(WaveManagement.Difficulty.Hard);
                    break;
                case var entry when entry == _diffBack:
                    SwitchMenu(_initVBox);
                    break;
            }
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
        }

        public override void _ExitTree()
        {
            // DisconnectSignals();
            base._ExitTree();
        }
    }
}
