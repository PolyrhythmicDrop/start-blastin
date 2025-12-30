using System;
using Godot;
using Interfaces;

[GlobalClass]
public partial class Shield : StaticBody2D, IDeflector, IListener
{
    private Sprite2D _sprite;
    private CollisionPolygon2D _collPoly;

    private Timer _shieldTimer;

    public Sprite2D Sprite => _sprite;
    public CollisionPolygon2D Polygon => _collPoly;

    public bool DeflectActive { get; set; } = true;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("%Sprite2D");
        _collPoly = GetNode<CollisionPolygon2D>("%CollisionPolygon2D");
        _shieldTimer = GetNode<Timer>("%Timer");
        Disable();

        ConnectSignals();
    }

    public void ConnectSignals()
    {
        _shieldTimer.Timeout += Disable;
    }

    public void DisconnectSignals()
    {
        _shieldTimer.Timeout -= Disable;
    }

    public void Enable()
    {
        _shieldTimer.Start();
        _sprite.Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        DeflectActive = true;
    }

    public void Disable()
    {
        _sprite.Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        DeflectActive = false;
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
