using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enemies;
using Events;
using Godot;
using Stats;
using Utility;
using Weapons;

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
    private event Action FireComplete;
    private event Action FinalFireComplete;

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

        ReadyBarrels();

        ConnectSignals();

        FollowPath(_path, _followSpeed);
    }

    private void ReadyBarrels()
    {
        foreach (Barrel barrel in _weapon?.Barrels)
        {
            barrel.ToggleActive(true);
        }
    }

    public override void ConnectSignals()
    {
        InitialFirePosReached += OnInitialFirePositionReached;
        FireWaypointReached += OnFireWaypointReached;
        FireComplete += OnFireComplete;
        FinalFireComplete += OnFinalFireComplete;
        base.ConnectSignals();
    }

    public override void DisconnectSignals()
    {
        InitialFirePosReached -= OnInitialFirePositionReached;
        FireWaypointReached -= OnFireWaypointReached;
        FireComplete -= OnFireComplete;
        FinalFireComplete -= OnFinalFireComplete;
        base.DisconnectSignals();
    }

    public override void _Process(double delta)
    {
        if (_alive)
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
        _alive = false;
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
        // Pause firing initially
        _weapon.FireTimer.Stop();

        float pathLength = path.Curve.GetBakedLength();
        float duration = Mathf.Max(pathLength / speed, 0.1f);

        if (_followTween != null)
        {
            _followTween.Kill();
        }

        // Start the tween
        _followTween = CreateTween();
        _followTween.TweenProperty(path.PathFollow, "progress_ratio", 1.0, duration);
    }

    private void CheckWaypoints()
    {
        KeyValuePair<Vector2, bool> initFirePos = _firePositions.ElementAt(0);
        KeyValuePair<Vector2, bool> finalFirePos = _firePositions.ElementAt(2);
        if (
            _path.PathFollow.Position.DistanceSquaredTo(initFirePos.Key) <= 30
            && initFirePos.Value == false
        )
        {
            InitialFirePosReached?.Invoke();
        }

        foreach (KeyValuePair<Vector2, bool> kvp in _firePositions)
        {
            if (_path.PathFollow.Position.DistanceSquaredTo(kvp.Key) <= 30 && kvp.Value == false)
            {
                _firePositions[kvp.Key] = true;
                FireWaypointReached?.Invoke(kvp.Key);
            }
        }
    }

    private void OnInitialFirePositionReached()
    {
        _path.PathFollow.Rotates = false;
        _firingBegun = true;
    }

    private async void OnFireWaypointReached(Vector2 waypoint)
    {
        // Pause movement for a spell
        _followTween.Pause();
        // Fire weapon
        FireWeapon();
        _weapon.FireTimer.Start();
        await StayAndSpin();
        _weapon.FireTimer.Stop();

        if (waypoint != _firePositions.ElementAt(2).Key)
        {
            FireComplete?.Invoke();
        }
        else if (!_flouncing)
        {
            FinalFireComplete?.Invoke();
        }
    }

    private void OnFireComplete()
    {
        _followTween.Play();
    }

    private void OnFinalFireComplete()
    {
        if (_flouncing)
        {
            return;
        }

        Flounce();
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
        float pathRotation = _path
            .Curve.SampleBakedWithRotation(
                _path.PathFollow.ProgressRatio * _path.Curve.GetBakedLength()
            )
            .Rotation;

        // Tween the spin
        if (_flounceTween != null)
        {
            _flounceTween.Kill();
        }
        _flounceTween = CreateTween();
        _flounceTween.TweenInterval(0.4f);
        _flounceTween.TweenProperty(_path.PathFollow, "rotation", pathRotation, 0.4f);

        // Resume following when the spin is complete
        await ToSignal(_flounceTween, Tween.SignalName.Finished);
        _path.PathFollow.Rotates = true;
        _followTween.Play();
    }

    public override void _ExitTree()
    {
        DisconnectSignals();
        base._ExitTree();
    }
}
