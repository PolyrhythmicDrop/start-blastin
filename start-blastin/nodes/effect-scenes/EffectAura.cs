using System;
using Effects;
using Godot;
using Interfaces;

[GlobalClass]
public partial class EffectAura : Area2D, IListener
{
    private CollisionShape2D _colShapeNode;
    private CircleShape2D _circleShape;

    public CollisionShape2D CollisionShapeNode => _colShapeNode;

    public CircleShape2D CircleShape => _circleShape;

    public Action<GodotObject> EffectEnableCallback;
    public Action<GodotObject> EffectDisableCallback;

    public override void _Ready()
    {
        _colShapeNode = GetNode<CollisionShape2D>("%AuraShape");
        _circleShape = (CircleShape2D)_colShapeNode.Shape;

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
