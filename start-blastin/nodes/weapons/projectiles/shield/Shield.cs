using System;
using Enemies;
using Entities;
using Environmental;
using Events;
using Godot;
using Interfaces;
using Projectiles;
using Utility;

[GlobalClass]
public partial class Shield : Projectile, IDeflector, IVelocityProvider
{
    // Nodes //
    private Sprite2D _sprite;
    private CollisionPolygon2D _collPoly;
    private StaticBody2D _staticBody;

    private ShapeCast2D _shapeCast;

    public ShapeCast2D ShapeCast => _shapeCast;

    // Position and physics //
    private Vector2 _currentTrajectory = Vector2.Zero;
    private Vector2 _currentVelocity = Vector2.Zero;
    private Vector2 _nextPosition;
    private Vector2 _lastPosition;

    // Shaders & Materials //
    private ShaderMaterial _deflectHitFXMaterial = ResourceLoader.Load<ShaderMaterial>(
        "uid://diss6xjfqle4y"
    );

    private ShaderMaterial _enemyPaletteMaterial = ResourceLoader.Load<ShaderMaterial>(
        "uid://bqux6tvmprg1l"
    );

    private Tween _deflectTween;

    // Public //
    public Sprite2D Sprite => _sprite;
    public CollisionPolygon2D Polygon => _collPoly;

    public bool DeflectActive { get; set; } = true;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("%Sprite2D");
        if (_faction == Faction.Enemies)
        {
            // Material = _deflectHitFXMaterial;
            // _sprite.Material = _enemyPaletteMaterial;
            _sprite.UseParentMaterial = false;
        }
        else
        {
            _sprite.UseParentMaterial = true;
        }
        _collPoly = GetNode<CollisionPolygon2D>("%CollisionPolygon2D");
        _shapeCast = GetNode<ShapeCast2D>("%ShapeCast2D");
        _lastPosition = GlobalPosition;
        base._Ready();
    }

    public override void ToggleActive(bool active)
    {
        ProcessMode = ProcessModeEnum.Inherit;
        DeflectActive = active;

        if (active)
        {
            if (_faction == Faction.Enemies && _sprite != null)
            {
                _sprite.UseParentMaterial = false;
            }
            if (Material is ShaderMaterial shader)
            {
                shader.SetShaderParameter("mix_ratio", 0);
            }
        }

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

    // public void OnCollision(CollisionEventArgs args)
    // {
    //     // if (args.Collider is EnemyNode or Player or Projectile)
    //     // {
    //     //     PlayDeflectAnimation();
    //     // }
    // }

    private void PlayDeflectAnimation()
    {
        // Material = _deflectHitFXMaterial;
        if (_faction is Faction.Enemies)
        {
            _sprite.UseParentMaterial = true;
        }

        if (Material is ShaderMaterial shader)
        {
            if (_deflectTween != null && _deflectTween.IsValid())
            {
                _deflectTween.Kill();
            }

            shader.SetShaderParameter("mix_ratio", 1.0);

            float startValue = GD.Randf() * 10;

            _deflectTween = CreateTween();

            _deflectTween.TweenMethod(
                Callable.From(
                    (int currentFrame) =>
                    {
                        shader.SetShaderParameter("current_frame", currentFrame);
                    }
                ),
                startValue,
                30,
                0.4f
            );
            _deflectTween.TweenCallback(
                Callable.From(() =>
                {
                    shader.SetShaderParameter("mix_ratio", 0.0);
                })
            );
            _deflectTween.TweenCallback(
                Callable.From(() =>
                {
                    if (_faction == Faction.Enemies)
                    {
                        _sprite.UseParentMaterial = false;
                    }
                })
            );

            // _deflectTween.Finished += () =>
            // {
            //     if (_faction == Faction.Enemies)
            //     {
            //         _sprite.UseParentMaterial = false;
            //     }
            // };
        }
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
        // SetCollisionMaskValue(1, false);
        // SetCollisionMaskValue(3, false);
    }

    public override void SetRayMask(Faction faction)
    {
        base.SetRayMask(faction);
        // _ray?.SetCollisionMaskValue(1, false);
        // _ray?.SetCollisionMaskValue(3, false);
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

        if (_shapeCast.IsColliding() && !_isBeingDeflected)
        {
            for (int i = 0; i < _shapeCast.CollisionResult.Count; i++)
            {
                GodotObject collider = _shapeCast.GetCollider(i);
                // Don't raise the collision if we're colliding with a projectile that is in the process of being deflected.
                if ((collider is Projectile proj && proj.IsBeingDeflected) || collider is OobArea)
                {
                    return;
                }

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

    public override void ConvertToNewFaction(Faction? faction = null)
    {
        base.ConvertToNewFaction(faction);
        if (_faction == Faction.Enemies)
        {
            _sprite.UseParentMaterial = false;
        }
        else if (_faction == Faction.Players)
        {
            _sprite.UseParentMaterial = true;
        }
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
