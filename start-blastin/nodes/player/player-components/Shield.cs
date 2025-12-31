using System;
using Godot;
using Interfaces;

[GlobalClass]
public partial class Shield : StaticBody2D, IDeflector, IListener, IVelocityProvider
{
    private Sprite2D _sprite;
    private CollisionPolygon2D _collPoly;

    private Timer _shieldTimer;

    public Sprite2D Sprite => _sprite;
    public CollisionPolygon2D Polygon => _collPoly;

    public float BlockTime { get; set; } = 0.5f;

    public bool DeflectActive { get; set; } = true;

    /// <summary>
    /// Is the shield active?
    /// </summary>
    public bool Enabled { get; set; } = true;

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
        Enabled = true;
        _shieldTimer.Start(BlockTime);
        _sprite.Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
        DeflectActive = true;
    }

    public void Disable()
    {
        Enabled = false;
        _sprite.Visible = false;
        ProcessMode = ProcessModeEnum.Disabled;
        DeflectActive = false;
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }

    public Vector2 GetCurrentVelocity()
    {
        return ConstantLinearVelocity;
    }
}
