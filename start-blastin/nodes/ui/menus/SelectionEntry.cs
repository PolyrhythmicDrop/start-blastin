using System;
using System.Threading.Tasks;
using Godot;

namespace UI
{
    // [GlobalClass]
    public partial class SelectionEntry : Control
    {
        public Func<Task> SelectAction;

        public Vector2 GetEntrySelectorPoint()
        {
            float yPos = GlobalPosition.Y + (Size.Y / 2);
            return new Vector2(GlobalPosition.X, yPos);
        }

        public void SetSelectAction(Func<Task> action)
        {
            SelectAction = action;
        }
    }
}
