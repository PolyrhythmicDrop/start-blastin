using System.Collections.Generic;
using Enemies;
using Entities;
using Godot;
using Projectiles;

[GlobalClass]
public partial class Missile : Projectile
{
    public static string ScenePath => "res://nodes/weapons/projectiles/missile/missile.tscn";
    private AnimatedSprite2D _sprite;

    /// <summary>
    /// Rotation speed per frame, in radians.
    /// </summary>
    private const float TURNRAD = 0.035f;

    // Targeting variables
    private Area2D _targetingArea;
    private Node2D _currentTarget;
    private Callable _nullTargetCallable;

    public override void _Ready()
    {
        base._Ready();
        _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        _targetingArea = GetNode<Area2D>("%TargetingArea");
        _nullTargetCallable = Callable.From(() =>
        {
            _currentTarget = null;
        });

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
            if (_sourceWeapon.EnemyOwned)
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
                        SetCurrentTarget((Player)found);
                    }
                }
                // If the missile is the player's, search for other enemies in the area.
                else
                {
                    Node2D found = bodies.Find(body => body is EnemyNode);
                    if (found != null)
                    {
                        SetCurrentTarget((EnemyNode)found);
                    }
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

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
