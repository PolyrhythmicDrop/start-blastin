using System;
using Godot;

namespace UI
{
    public partial class DifficultySelectionMenu : SelectionMenu
    {
        private SelectionEntry _easy;
        private SelectionEntry _medium;
        private SelectionEntry _hard;
        private SelectionEntry _back;

        public SelectionEntry Easy => _easy;
        public SelectionEntry Medium => _medium;
        public SelectionEntry Hard => _hard;
        public SelectionEntry Back => _back;

        public override void _Ready()
        {
            base._Ready();
            // _currentEntryIndex = 0;

            _easy = GetNode<SelectionEntry>("%Easy");
            _medium = GetNode<SelectionEntry>("%Medium");
            _hard = GetNode<SelectionEntry>("%Hard");
            _back = GetNode<SelectionEntry>("%Back");
        }
    }
}
