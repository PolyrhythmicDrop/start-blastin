using System;
using Godot;

namespace Components
{
    [GlobalClass]
    public partial class EntityPath : Path2D
    {
        protected PathFollow2D _pathFollow;

        [Export]
        public float FollowRatio
        {
            get => _pathFollow.ProgressRatio;
            set
            {
                _pathFollow.ProgressRatio = value;
                if (value >= 1.0)
                {
                    PathComplete?.Invoke();
                }
            }
        }

        public PathFollow2D PathFollow => _pathFollow;

        public event Action PathComplete;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _pathFollow = GetNode<PathFollow2D>("%PathFollow2D");
        }
    }
}
