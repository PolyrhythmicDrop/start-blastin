using System;
using System.Threading.Tasks;
using Godot;
using WaveManagement;

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

        /// <summary>
        /// Assigns callback functions to each <see cref="SelectionEntry"/> in the Difficulty menu.
        /// </summary>
        /// <param name="onDiffSelect">Callback function for the difficulty option.</param>
        /// <param name="onBackSelect">Callback function for the Back option.</param>
        public void SetDifficultyActions(
            Func<Difficulty, Task> onDiffSelect,
            Func<Task> onBackSelect
        )
        {
            Easy.SetSelectAction(async () => await onDiffSelect(Difficulty.Easy));
            Medium.SetSelectAction(async () => await onDiffSelect(Difficulty.Medium));
            Hard.SetSelectAction(async () => await onDiffSelect(Difficulty.Hard));

            Back.SetSelectAction(onBackSelect);
        }
    }
}
