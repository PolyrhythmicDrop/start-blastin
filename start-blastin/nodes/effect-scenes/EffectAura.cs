using System;
using Godot;
using Interfaces;

[Tool]
[GlobalClass]
public partial class EffectAura : Area2D, IListener
{
    [Export]
    public TextureRect AuraTexture { get; set; }

    [Export]
    public CollisionShape2D CollisionShapeNode { get; set; }

    [Export]
    public CircleShape2D CircleShape
    {
        get
        {
            if (CollisionShapeNode != null)
            {
                return (CircleShape2D)CollisionShapeNode.Shape;
            }
            else
            {
                return null;
            }
        }
        set
        {
            if (CollisionShapeNode != null)
            {
                CollisionShapeNode.Shape = value;
            }
        }
    }

    public Action<GodotObject> EffectEnableCallback;
    public Action<GodotObject> EffectDisableCallback;

    [Export]
    public float AuraRadius
    {
        get
        {
            if (CircleShape != null)
            {
                return CircleShape.Radius;
            }
            else
            {
                return 0;
            }
        }
        set => ChangeAuraShape(radius: value);
    }

    public override void _Ready()
    {
        ConnectSignals();
    }

    public void ConnectSignals()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public void DisconnectSignals()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
    }

    public void ChangeAuraShape(float? radius = null, Vector2? minSize = null)
    {
        if (radius != null)
        {
            if (CircleShape != null)
            {
                CircleShape.Radius = (float)radius;
                // Convert the radius to Vector2
                AuraTexture.CustomMinimumSize = new Vector2(
                    (float)(radius * 2),
                    (float)(radius * 2)
                );
            }
        }
        else if (minSize != null)
        {
            if (CircleShape != null)
            {
                Vector2 minVect = (Vector2)minSize;
                AuraTexture.CustomMinimumSize = minVect;
                // Convert the radius into the min vect
                CircleShape.Radius = minVect.X / 2;
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        EffectEnableCallback(body);
    }

    private void OnBodyExited(Node2D body)
    {
        EffectDisableCallback(body);
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
