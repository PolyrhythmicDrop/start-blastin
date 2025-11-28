using System;
using System.Collections.Generic;
using System.Linq;
using Enemies;
using Entities;
using Godot;
using Projectiles;
using Utility;

[GlobalClass]
public partial class Missile : Projectile
{
    public static string ScenePath => "res://nodes/weapons/projectiles/missile/missile.tscn";
    private AnimatedSprite2D _sprite;

    /// <summary>
    /// Rotation speed per frame, in radians.
    /// </summary>
    private const float TURNSPEED = 0.04f;

    // Targeting variables
    private Area2D _targetingArea;
    private Node2D _currentTarget;

    public override void _Ready()
    {
        base._Ready();
        _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        _targetingArea = GetNode<Area2D>("%TargetingArea");

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

    public override void _Process(double delta) { }

    protected override Vector2 SetTrajectory(double delta)
    {
        Vector2 fireVector;
        if (_currentTarget == null)
        {
            fireVector = Vector2.Right.Rotated(GlobalRotation);
        }
        else
        {
            // Get the angle to the target, given no rotation
            float angleToTarget = GlobalPosition.AngleToPoint(_currentTarget.GlobalPosition);
            // Get the difference between the angle to target and the current rotation
            float angleDelta = angleToTarget - GlobalRotation;

            float rotationThisFrame;
            // Snap to the target if we're within the turn radius
            if (Mathf.Abs(angleDelta) <= TURNSPEED)
            {
                rotationThisFrame = angleDelta;
            }
            else
            {
                rotationThisFrame = Mathf.Sign(angleDelta) * TURNSPEED;
            }

            Rotate(rotationThisFrame);
            fireVector = Vector2.Right.Rotated(GlobalRotation);
        }
        return _currentSpeed * (float)delta * fireVector;
    }

    public void OnTargetAreaEntered(Node2D body)
    {
        if (_currentTarget == null)
        {
            if (_sourceWeapon.EnemyOwned)
            {
                if (body is Player player && _currentTarget != player)
                {
                    _currentTarget = player;
                }
            }
            else
            {
                if (body is EnemyNode enemy && _currentTarget != enemy)
                {
                    _currentTarget = enemy;
                }
            }
        }
        DebugLogger.LogMessage($"{Name} current target: {_currentTarget?.Name}", true);
    }

    public void OnTargetAreaExited(Node2D body)
    {
        if (body == _currentTarget)
        {
            // If the current target left the area, reset the current target
            _currentTarget = null;

            // Find if there are other overlapping bodies in the area.
            if (_targetingArea.HasOverlappingBodies())
            {
                // Compile the bodies into a searchable list
                List<Node2D> bodies = [.. _targetingArea.GetOverlappingBodies()];

                // If the missile was fired from an enemy, see if any of the bodies are a player, then track that player.
                if (_sourceWeapon.EnemyOwned)
                {
                    Node2D found = bodies.Find(body => body is Player);
                    if (found != null)
                    {
                        _currentTarget = (Player)found;
                    }
                }
                // If the missile is the player's, search for other enemies in the area.
                else
                {
                    Node2D found = bodies.Find(body => body is EnemyNode);
                    if (found != null)
                    {
                        _currentTarget = (EnemyNode)found;
                    }
                }
            }
            DebugLogger.LogMessage($"{Name} current target: {_currentTarget?.Name}", true);
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

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
