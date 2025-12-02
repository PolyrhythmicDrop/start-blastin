using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enemies;
using Godot;
using Utility;

[GlobalClass]
public partial class Salvo : EnemyNode
{
    private Node2D _spriteContainer;
    private AnimatedSprite2D _body;
    private AnimatedSprite2D _engine;
    private AnimatedSprite2D _rack;
    private AnimatedSprite2D _destruction;

    // Waypointing and path following
    // private List<Vector2> _firingPositions = new();
    private Dictionary<Vector2, bool> _firePositions = new();
    private bool _firingBegun = false;
    private Tween _followTween;
    private Tween _spinTween;

    private event Action<Vector2> FirePositionReached;

    public override void _Ready()
    {
        base._Ready();
        _spriteContainer = GetNode<Node2D>("%SpriteContainer");
        _body = GetNode<AnimatedSprite2D>("%Body");
        _engine = GetNode<AnimatedSprite2D>("%Engine");
        _rack = GetNode<AnimatedSprite2D>("%Rack");
        _destruction = GetNode<AnimatedSprite2D>("%Destruction");

        _currentGlobalPosition = GlobalPosition;
        _lastGlobalPosition = _currentGlobalPosition;

        // Get the points that I need to know about.
        _firePositions.Add(_path.Curve.GetPointPosition(1), false);
        _firePositions.Add(_path.Curve.GetPointPosition(2), false);
        _firePositions.Add(_path.Curve.GetPointPosition(3), false);

        FollowPath(_path, _followSpeed);
    }

    public override void _Process(double delta)
    {
        _lastGlobalPosition = _currentGlobalPosition;
        _currentGlobalPosition = GlobalPosition;

        CheckWaypoints();

        base._Process(delta);
        SetMoveAnimation();

        KinematicCollision2D collision = MoveAndCollide(_motion, true);

        if (collision != null)
        {
            OnCrash(collision);
        }
    }

    private void SetMoveAnimation()
    {
        int sign = Math.Sign(_currentVelocity.X);
        switch (sign)
        {
            // If we're moving left
            case -1:
                _engine.Play("strafe-left");
                break;
            // If we're moving right
            case 1:
                _engine.Play("strafe-right");
                break;
            // If we're standing still
            case 0:
            default:
                _engine.Play("idle");
                break;
        }
    }

    protected override void FireWeapon()
    {
        base.FireWeapon();
        _rack.Play("fire");
    }

    public override void PlayDamageAnimation()
    {
        string mixRatioPath = "mix_ratio";
        string currentFramePath = "current_frame";

        if (_spriteContainer.Material is ShaderMaterial shaderMaterial)
        {
            shaderMaterial.SetShaderParameter(mixRatioPath, 1.0);

            Tween tween = _spriteContainer.CreateTween();
            tween.TweenMethod(
                Callable.From(
                    (int currentFrame) =>
                        shaderMaterial.SetShaderParameter(currentFramePath, currentFrame)
                ),
                0,
                30,
                0.5
            );
            tween.TweenCallback(
                Callable.From(() => shaderMaterial.SetShaderParameter(mixRatioPath, 0))
            );
        }
    }

    public override void Die(int? playerId = null)
    {
        _weapon.FireTimer.Stop();
        _shape.Disabled = true;

        // Make the non-destruction sprites invisible and turn on the destruction sprite
        _body.Visible = false;
        _engine.Visible = false;
        _rack.Visible = false;

        _destruction.Visible = true;
        _destruction.Play();

        _destruction.AnimationFinished += () => base.Die(playerId);
    }

    protected override void FollowPath(EntityPath path, float speed)
    {
        // This should follow three main phases:
        // 1. Get to the firing position (no firing during this time)
        // 2. Strafe along the firing line whilst firing.
        // 3. Spin and depart.

        // Pause firing initially
        _weapon.FireTimer.Stop();

        float pathLength = path.Curve.GetBakedLength();
        float duration = Mathf.Max(pathLength / speed, 0.1f);

        // Start the tween
        _followTween = CreateTween();
        _followTween.TweenProperty(path.PathFollow, "progress_ratio", 1.0, duration);
    }

    private void CheckWaypoints()
    {
        foreach (KeyValuePair<Vector2, bool> kvp in _firePositions)
        {
            if (_path.PathFollow.Position.DistanceSquaredTo(kvp.Key) <= 30 && kvp.Value == false)
            {
                ToggleFirePattern(true);
                _firePositions[kvp.Key] = true;
            }
        }

        // Resume rotation after the last fire position
        if (_firePositions.ElementAt(2).Value == true && _firingBegun == true)
        {
            ToggleFirePattern(false);
        }
    }

    private async Task<bool> StayAndSpin()
    {
        float initRotDeg = RotationDegrees;
        float rotateAmount = 45f;
        _spinTween = CreateTween();
        _spinTween.TweenProperty(this, "rotation_degrees", initRotDeg + rotateAmount, 1);
        _spinTween.TweenProperty(this, "rotation_degrees", initRotDeg - rotateAmount, 1);
        _spinTween.TweenProperty(this, "rotation_degrees", initRotDeg, 0.5);
        await ToSignal(_spinTween, Tween.SignalName.Finished);
        return true;
    }

    private async void ToggleFirePattern(bool fire)
    {
        if (fire)
        {
            _firingBegun = true;
            _path.PathFollow.Rotates = false;
            // Fire
            FireWeapon();
            _weapon.FireTimer.Start();
            // Pause movement for a spell
            _followTween.Pause();
            // Spin and fire
            bool spinComplete = await StayAndSpin();
            if (spinComplete)
            {
                // Stop firing and continue movement.
                _weapon.FireTimer.Stop();
                _followTween.Play();
            }
        }
        else
        {
            _firingBegun = false;
            float offset = _path.Curve.GetClosestOffset(_firePositions.ElementAt(2).Key);
            // Tween tween = CreateTween();
            _spinTween.TweenInterval(0.3f);
            _spinTween.TweenProperty(
                _path.PathFollow,
                "rotation",
                _path.Curve.SampleBakedWithRotation(offset).Rotation,
                0.3f
            );
            await ToSignal(_spinTween, Tween.SignalName.Finished);
            _path.PathFollow.Rotates = true;
        }
    }
}
