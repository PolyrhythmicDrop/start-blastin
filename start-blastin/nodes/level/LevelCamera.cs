using System;
using System.Drawing;
using Godot;
using Utility;

public partial class LevelCamera : Camera2D
{
    public const int width = 1920;
    public const int height = 1080;

    public float ratio = 1920 / 1080;
    public Vector2 target = Vector2.One;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetViewport().SizeChanged += OnSizeChanged;
        GetWindow().MinSize = new Vector2I(width, height);
    }

    public void OnSizeChanged()
    {
        Vector2I windowSize = GetWindow().Size;
        if (windowSize.Y % 2 == 0)
        {
            windowSize.Y += 1;
        }
        windowSize.X = (int)(windowSize.Y * ratio);
        var targetSize = new Vector2(windowSize.X, windowSize.Y) / new Vector2(width, height);
        target = targetSize;
        DebugLogger.LogMessage($"Window size changed! New target: {target}", true);
    }

    public override void _Process(double delta)
    {
        if (!Zoom.IsEqualApprox(target))
        {
            Zoom = Zoom.Lerp(target, 0.2f);
        }
        else
        {
            Zoom = target;
        }
    }
}
