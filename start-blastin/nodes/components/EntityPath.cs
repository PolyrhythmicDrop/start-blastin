using Godot;

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

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    /// <summary>
    /// Makes any child of this EntityPath follow the path at a set speed.
    /// </summary>
    /// <param name="speed">The speed at which the child should follow the path.</param>
    public virtual void FollowPath(float speed)
    {
        float pathLength = Curve.GetBakedLength();
        float duration = Mathf.Max(pathLength / speed, 0.1f);

        Tween tween = CreateTween();
        tween.TweenProperty(_pathFollow, "progress_ratio", 1.0, duration);
    }
}
