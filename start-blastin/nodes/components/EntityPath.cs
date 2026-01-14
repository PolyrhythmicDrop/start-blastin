using Godot;

namespace Components
{
    public partial class EntityPath : Path2D
    {
        protected PathFollow2D _pathFollow;

        public static string ScenePath => "res://nodes/components/EntityPath.tscn";

        public PathFollow2D PathFollow => _pathFollow;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _pathFollow = GetNode<PathFollow2D>("%PathFollow2D");
        }

        public bool EntityIsAtPoint(Node2D entity, Vector2 point)
        {
            return entity.GlobalPosition == point;
        }
    }
}
