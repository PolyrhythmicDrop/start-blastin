using System.Collections.Generic;
using System.Diagnostics;
using Enemies;
using Entities;
using Events;
using Godot;
using Interfaces;
using Projectiles;
using Utility;

[GlobalClass]
public partial class Missile : Projectile
{
    private AnimatedSprite2D _sprite;

    /// <summary>
    /// Rotation speed per frame, in radians.
    /// </summary>
    private const float TURNRAD = 0.025f;

    private ShapeCast2D _shapeCast;

    // Targeting variables
    private Area2D _targetingArea;
    private Node2D _currentTarget;
    private Callable _nullTargetCallable;

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        _targetingArea = GetNode<Area2D>("%TargetingArea");
        _shapeCast = GetNode<ShapeCast2D>("%ShapeCast2D");

        _nullTargetCallable = Callable.From(RemoveCurrentTarget);
        base._Ready();

        ConnectSignals();
    }

    public void ConnectSignals()
    {
        _targetingArea.BodyEntered += OnTargetAreaEntered;
        _targetingArea.BodyExited += OnTargetAreaExited;
    }

    public void DisconnectSignals()
    {
        // _targetingArea.BodyEntered -= OnTargetAreaEntered;
        // _targetingArea.BodyExited -= OnTargetAreaExited;
    }

    // Shapecast changes

    public override void SetRayMask(Faction faction)
    {
        base.SetRayMask(faction);
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

        Vector2 nextPos = GlobalPosition + GetTrajectory(delta);
        _shapeCast.TargetPosition = ToLocal(nextPos);

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
        // else
        // {
        //     collNormal *= -1;
        // }

        return new CollisionEventArgs(collider, collPoint, collNormal);
    }

    protected override Vector2 GetTrajectory(double delta)
    {
        if (_currentTarget != null)
        {
            // Get the angle to the target in global space
            float angleToTarget = GlobalPosition.AngleToPoint(_currentTarget.GlobalPosition);
            // Get the difference between the angle to target and the current rotation
            float angleDelta = angleToTarget - GlobalRotation;

            // Handle wrap-around from the angleToTarget turning negative past Pi
            if (angleDelta > Mathf.Pi)
            {
                angleDelta -= Mathf.Tau;
            }
            else if (angleDelta < -Mathf.Pi)
            {
                angleDelta += Mathf.Tau;
            }

            // Set the rotation for this frame to the turn radius, positive or negative depending on the sign of the delta
            float rotationThisFrame = Mathf.Sign(angleDelta) * TURNRAD;

            // Snap to the target if we're within the turn radius
            if (Mathf.Abs(angleDelta) <= TURNRAD * 2)
            {
                rotationThisFrame = angleDelta;
            }

            // Rotate the missile
            GlobalRotation += rotationThisFrame;
        }

        // Set the direction vector according to the current rotation.
        Vector2 directionVector = Vector2.Right.Rotated(GlobalRotation).Normalized();
        if (Mathf.Abs(directionVector.X) < 0.0001f)
        {
            directionVector.X = 0;
        }
        if (Mathf.Abs(directionVector.Y) < 0.0001f)
        {
            directionVector.Y = 0;
        }

        return _currentSpeed * (float)delta * directionVector;
    }

    public void OnTargetAreaEntered(Node2D body)
    {
        if (_currentTarget == null)
        {
            // if (_sourceWeapon.EnemyOwned)
            // If this is an enemy projectile...
            if (GetCollisionLayerValue(5) && !GetCollisionLayerValue(4))
            {
                if (body is Player player && _currentTarget != player)
                {
                    SetCurrentTarget(player);
                }
            }
            else
            {
                if (body is EnemyNode enemy && _currentTarget != enemy)
                {
                    SetCurrentTarget(enemy);
                }
            }
        }
    }

    private void SetCurrentTarget(Node2D target = null)
    {
        if (target != null)
        {
            if (!target.IsConnected(SignalName.TreeExiting, _nullTargetCallable))
            {
                target.Connect(SignalName.TreeExiting, _nullTargetCallable);
            }
        }
        _currentTarget = target;
    }

    public void OnTargetAreaExited(Node2D body)
    {
        if (body != _currentTarget)
        {
            return;
        }
        else
        {
            RemoveCurrentTarget();
        }
    }

    public void RemoveCurrentTarget()
    {
        _currentTarget = null;
        FindNewTarget();
    }

    public void FindNewTarget()
    {
        // Find if there are other overlapping bodies in the area.
        if (_targetingArea.HasOverlappingBodies())
        {
            // Compile the bodies into a searchable list
            List<Node2D> bodies = [.. _targetingArea.GetOverlappingBodies()];

            // If the missile was fired from an enemy, see if any of the bodies are a player, then track that player.
            if (_faction == Faction.Enemies || _faction == Faction.All)
            {
                Node2D found = bodies.Find(body => body is Player);
                if (found != null)
                {
                    SetCurrentTarget((Player)found);
                }
            }
            // If the missile is the player's, search for other enemies in the area.
            else if (_faction == Faction.Players || _faction == Faction.All)
            {
                Node2D found = bodies.Find(body => body is EnemyNode);
                if (found != null)
                {
                    SetCurrentTarget((EnemyNode)found);
                }
            }
        }
    }

    public override void ToggleActive(bool active)
    {
        if (!active)
        {
            _currentTarget = null;
            GlobalRotation = _sourceWeapon.GlobalRotation;
        }
        base.ToggleActive(active);
    }

    public override void Deflect(IDeflector deflector, CollisionEventArgs args = null)
    {
        base.Deflect(deflector, args);
        RemoveCurrentTarget();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
