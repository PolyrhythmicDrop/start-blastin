using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Autoloads;
using Godot;
using Interfaces;
using Utility;

namespace UI
{
    public partial class StartupScreen : Node
    {
        private bool _initialized = false;
        private AnimatedSprite2D _selector;

        private RichTextLabel _newGameLabel;
        private RichTextLabel _optionsLabel;

        private List<Control> _entryList;

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

            _newGameLabel = GetNode<RichTextLabel>("%NewGame");
            _optionsLabel = GetNode<RichTextLabel>("%Options");

            // ConnectSignals();
        }

        private void Initialize()
        {
            _initialized = true;

            _entryList = [_newGameLabel, _optionsLabel];
            CurrentEntryIndex = 0;
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
        }

        private void MakeSelection()
        {
            switch (_entryList[_currentEntryIndex])
            {
                case var entry when entry == _newGameLabel:
                    SceneManager.Instance.LoadNewGame();
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
