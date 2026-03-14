using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Utility;

namespace UI
{
    // [GlobalClass]
    public partial class SelectionMenu : Control
    {
        private Tween _selTween;

        private ColorPalette _alphaPalette = ResourceLoader.Load<ColorPalette>(
            "uid://jo6hbayjhfba"
        );

        protected List<SelectionEntry> _entryList = new();

        protected int _currentEntryIndex = 0;
        public int CurrentEntryIndex
        {
            get => _currentEntryIndex;
            set { _currentEntryIndex = value; }
        }

        public SelectionEntry CurrentEntry => _entryList?[_currentEntryIndex] ?? null;

        /// <summary>
        /// Whether or not the <see cref="_currentEntryIndex"/> should reset when moving off the menu.
        /// </summary>
        public bool RememberEntry { get; set; } = false;

        public override void _Ready()
        {
            var c = GetChildren();
            foreach (SelectionEntry s in c)
            {
                _entryList.Add(s);
            }
            _currentEntryIndex = 0;
        }

        public virtual void SetEntrySelectAction(SelectionEntry entry, Func<Task> action)
        {
            entry.SetSelectAction(action);
        }

        public void MakeSelection()
        {
            if (CurrentEntry?.SelectAction != null)
            {
                CurrentEntry.SelectAction();
            }
            else
            {
                DebugLogger.LogMessage(
                    $"Either {nameof(CurrentEntry)} is null or {nameof(CurrentEntry.SelectAction)} is null. Did you remember to set a callback function to SelectAction?",
                    true,
                    true
                );
            }
        }

        public void IncrementCurrentIndex(Node2D selector)
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
            // MoveSelectorToEntry(CurrentEntry, selector);
        }

        public void DecrementCurrentIndex(Node2D selector)
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
            // MoveSelectorToEntry(CurrentEntry, selector);
        }

        // private void MoveSelectorToEntry(SelectionEntry entry, Node2D selector)
        // {
        //     if (entry == null)
        //     {
        //         return;
        //     }

        //     // if (_selTween != null && _selTween.IsValid())
        //     // {
        //     //     _selTween.Kill();
        //     // }

        //     selector.GlobalPosition = entry.GetEntrySelectorPoint();
        //     // TweenSelectorIdle();
        // }

        // private Vector2 GetEntrySelectorPoint(int index)
        // {
        //     Control entry = _entryList[index];
        //     Vector2 cSize = entry.Size;
        //     float yPos = entry.GlobalPosition.Y + (cSize.Y / 2);
        //     return new Vector2(entry.GlobalPosition.X, yPos);
        // }
    }
}
