using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enemies;
using Godot;

[GlobalClass]
public partial class Salvo : EnemyNode
{
    private Node2D _spriteContainer;
    private AnimatedSprite2D _body;
    private AnimatedSprite2D _engine;
    private AnimatedSprite2D _rack;
    private AnimatedSprite2D _destruction;

    // Waypointing and path following
    private Dictionary<Vector2, bool> _firePositions = new();
    private bool _firingBegun = false;
    private bool _flouncing = false;

    private Tween _spinTween;
    private Tween _flounceTween;

    private event Action InitialFirePosReached;
    private event Action<Vector2> FireWaypointReached;

    protected override void OnBaseReadyComplete()
    {
        _spriteContainer = GetNode<Node2D>("%SpriteContainer");
        _body = GetNode<AnimatedSprite2D>("%Body");
        _engine = GetNode<AnimatedSprite2D>("%Engine");
        _rack = GetNode<AnimatedSprite2D>("%Rack");
        _destruction = GetNode<AnimatedSprite2D>("%Destruction");

        // Get the points that I need to know about.
        _firePositions.Add(_followPath.Curve.GetPointPosition(1), false);
        _firePositions.Add(_followPath.Curve.GetPointPosition(2), false);
        _firePositions.Add(_followPath.Curve.GetPointPosition(3), false);

        _weaponComponent.ActivateAllBarrels();

        ConnectWaypointSignals();
    }

    protected override AnimatedSprite2D GetPrimarySprite() => _body;

    public void ConnectWaypointSignals()
    {
        InitialFirePosReached += OnInitialFirePositionReached;
        FireWaypointReached += OnFireWaypointReached;
    }

    public void DisconnectWaypointSignals()
    {
        InitialFirePosReached -= OnInitialFirePositionReached;
        FireWaypointReached -= OnFireWaypointReached;
    }

    protected override void OnProcessUpdate(double delta)
    {
        if (_alive)
        {
            CheckWaypoints();
            SetMoveAnimation();
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

    public override void PlayFireAnimation()
    {
        _rack.Play("fire");
    }

    protected override async Task PlayDeathSequence()
    {
        // Make the non-destruction sprites invisible and turn on the destruction sprite
        _body.Visible = false;
        _engine.Visible = false;
        _rack.Visible = false;

        // Play destruction sound and animation
        _audioComponent.PlayDestructionSound();
        _destruction.Visible = true;
        _destruction.Play();

        await ToSignal(_destruction, AnimatedSprite2D.SignalName.AnimationFinished);
    }

    protected override void FollowPath(float speed)
    {
        // Pause firing initially
        // _weapon.FireTimer.Stop();
        _weaponComponent.StopFiring();

        float pathLength = _followPath.Curve.GetBakedLength();
        float duration = Mathf.Max(pathLength / speed, MIN_FOLLOW_TWEEN_DURATION);

        if (_followTween != null)
        {
            _followTween.Kill();
        }

        // Start the tween
        _followTween = CreateTween();
        _followTween.TweenProperty(_followPath.PathFollow, "progress_ratio", 1.0, duration);
    }

    private void CheckWaypoints()
    {
        KeyValuePair<Vector2, bool> initFirePos = _firePositions.ElementAt(0);
        KeyValuePair<Vector2, bool> finalFirePos = _firePositions.ElementAt(2);
        if (
            _followPath.PathFollow.Position.DistanceSquaredTo(initFirePos.Key) <= 30
            && initFirePos.Value == false
        )
        {
            InitialFirePosReached?.Invoke();
        }

        foreach (KeyValuePair<Vector2, bool> kvp in _firePositions)
        {
            if (
                _followPath.PathFollow.Position.DistanceSquaredTo(kvp.Key) <= 30
                && kvp.Value == false
            )
            {
                _firePositions[kvp.Key] = true;
                FireWaypointReached?.Invoke(kvp.Key);
            }
        }
    }

    private void OnInitialFirePositionReached()
    {
        _followPath.PathFollow.Rotates = false;
        _firingBegun = true;
    }

    private async void OnFireWaypointReached(Vector2 waypoint)
    {
        // Pause movement for a spell
        _followTween.Pause();

        // Start firing while we stay in place and do our little spin move, then stop firing when we're done.
        _weaponComponent.StartFiring();
        await StayAndSpin();
        _weaponComponent.StopFiring();

        // If we're not at the final waypoint, play the follow tween.
        if (waypoint != _firePositions.ElementAt(2).Key)
        {
            _followTween.Play();
        }
        // If we're at the final waypoint but have not yet begun to flounce, flounce.
        else if (!_flouncing)
        {
            Flounce();
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

    private async void Flounce()
    {
        _flouncing = true;

        _followTween.Pause();

        // Get the offset at the current progress ratio
        float pathRotation = _followPath
            .Curve.SampleBakedWithRotation(
                _followPath.PathFollow.ProgressRatio * _followPath.Curve.GetBakedLength()
            )
            .Rotation;

        // Tween the spin
        if (_flounceTween != null)
        {
            _flounceTween.Kill();
        }
        _flounceTween = CreateTween();
        _flounceTween.TweenInterval(0.4f);
        _flounceTween.TweenProperty(_followPath.PathFollow, "rotation", pathRotation, 0.4f);

        // Resume following when the spin is complete
        await ToSignal(_flounceTween, Tween.SignalName.Finished);
        _followPath.PathFollow.Rotates = true;
        _followTween.Play();
    }

    public override void _ExitTree()
    {
        DisconnectWaypointSignals();
        base._ExitTree();
    }
}
