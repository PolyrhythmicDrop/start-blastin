using System;
using Events;
using Godot;
using Interfaces;
using Projectiles;
using Utility;

[GlobalClass]
public partial class Shield : Projectile, IDeflector, IVelocityProvider
{
    private Sprite2D _sprite;
    private CollisionPolygon2D _collPoly;
    private StaticBody2D _staticBody;
    private ShapeCast2D _shapeCast;

    private Vector2 _currentTrajectory = Vector2.Zero;
    private Vector2 _currentVelocity = Vector2.Zero;
    private Vector2 _nextPosition;
    private Vector2 _lastPosition;

    public Sprite2D Sprite => _sprite;
    public CollisionPolygon2D Polygon => _collPoly;

    public bool DeflectActive { get; set; } = true;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("%Sprite2D");
        _collPoly = GetNode<CollisionPolygon2D>("%CollisionPolygon2D");
        _shapeCast = GetNode<ShapeCast2D>("%ShapeCast2D");
        _lastPosition = GlobalPosition;
        base._Ready();
    }

    public override void ToggleActive(bool active)
    {
        ProcessMode = ProcessModeEnum.Inherit;
        DeflectActive = active;

        base.ToggleActive(active);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_active)
        {
            base._PhysicsProcess(delta);
            if (delta > 0)
            {
                _currentVelocity = (GlobalPosition - _lastPosition) / (float)delta;
            }
            _lastPosition = GlobalPosition;
        }
    }

    protected override void ToggleCollisionSignalConnection(bool connect)
    {
        base.ToggleCollisionSignalConnection(connect);
    }

    protected override Vector2 GetTrajectory(double delta)
    {
        _currentTrajectory = base.GetTrajectory(delta);
        return _currentTrajectory;
    }

    public override void SetProjectileCollisionLayers(Faction faction)
    {
        base.SetProjectileCollisionLayers(faction);

        // Turn off collisions with enemies and player.
        SetCollisionMaskValue(1, false);
        SetCollisionMaskValue(3, false);
    }

    public override void SetRayMask(Faction faction)
    {
        base.SetRayMask(faction);
        _ray?.SetCollisionMaskValue(1, false);
        _ray?.SetCollisionMaskValue(3, false);
        if (_shapeCast != null && _ray != null)
        {
            _shapeCast.CollisionMask = _ray.CollisionMask;
            // Disable the ray, since we're going to use the shapecast for collision instead.
            _ray.Enabled = false;
        }
    }

    protected override void CastRay(double delta)
    {
        _ray.Enabled = false;

        _nextPosition = GlobalPosition + GetTrajectory(delta);
        _shapeCast.TargetPosition = ToLocal(_nextPosition);

        if (_shapeCast.Enabled == false)
        {
            _shapeCast.Enabled = true;
        }

        _shapeCast.ForceShapecastUpdate();

        if (_shapeCast.IsColliding())
        {
            for (int i = 0; i < _shapeCast.CollisionResult.Count; i++)
            {
                RaiseCollision(this, CalculateShapeCollisionData(delta, i));
            }
        }
    }

    protected CollisionEventArgs CalculateShapeCollisionData(double delta, int index)
    {
        Vector2 collNormal = _shapeCast.GetCollisionNormal(index);
        Vector2 collPoint = _shapeCast.GetCollisionPoint(index);
        GodotObject collider = _shapeCast.GetCollider(index);

        // If we get a 0 normal (likely because the ray started inside the collider), calculate the normal manually.
        if (collNormal == Vector2.Zero)
        {
            // Calculate normal from relative position for a Node2D
            if (collider is Node2D collNode)
            {
                // Get the direction from the center of the collider to the Projectile.
                Vector2 collDir = (GlobalPosition - collNode.GlobalPosition).Normalized();
                collNormal = collDir;
            }
            else
            {
                // Otherwise, just use the opposite direction of the projectile
                collNormal = Vector2.Right.Rotated(GlobalRotation).Normalized() * -1;
            }
        }

        return new CollisionEventArgs(collider, collPoint, collNormal);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    public Vector2 GetCurrentVelocity()
    {
        return _currentVelocity;
    }
}
