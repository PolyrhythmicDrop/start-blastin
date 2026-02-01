using System.Threading.Tasks;
using Components;
using Enemies;
using Godot;

[GlobalClass]
public partial class Bumper : EnemyNode
{
    private Node2D _spriteContainer;

    private AnimatedSprite2D _bodySprite;

    private Node2D _rotator;
    private AnimatedSprite2D _turretSprite;
    private AnimatedSprite2D _shieldGenSprite;

    private StaticDeflector _deflectorN;
    private StaticDeflector _deflectorS;

    // ~ Rotation Variables ~ //

    /// <summary>
    /// The timer that determines when to rotate the barrels.
    /// </summary>
    private Timer _rotateTimer;

    private Tween _rotateTween;

    /// <summary>
    /// The time until the next rotation.
    /// </summary>
    private const float NEXT_ROTATION = 3;

    /// <summary>
    /// The time it takes to rotate the rotator component.
    /// </summary>
    private const float ROTATE_DURATION = 2;

    /// <summary>
    /// The amount to rotate (in degrees) each time a rotation is performed.
    /// </summary>
    private const int ROTATE_STEP = 90;

    protected override void OnBaseReadyComplete()
    {
        // Set all the child nodes
        _spriteContainer = GetNode<Node2D>("%SpriteContainer");
        _bodySprite = _spriteContainer.GetNode<AnimatedSprite2D>("%BodySprite");
        _rotator = _spriteContainer.GetNode<Node2D>("%Rotator");
        _turretSprite = _rotator.GetNode<AnimatedSprite2D>("%TurretSprite");
        _shieldGenSprite = _rotator.GetNode<AnimatedSprite2D>("%ShieldGenSprite");
        _deflectorN = _shieldGenSprite.GetNode<StaticDeflector>("%DeflectorN");
        _deflectorS = _shieldGenSprite.GetNode<StaticDeflector>("%DeflectorS");

        InitializeTimers();
    }

    /// <summary>
    /// Sets up and starts the rotation timer.
    /// </summary>
    private void InitializeTimers()
    {
        _rotateTimer = new()
        {
            WaitTime = NEXT_ROTATION,
            OneShot = true,
            Autostart = false,
        };
        _rotateTimer.Timeout += RotateRotator;

        _rotator.AddChild(_rotateTimer);

        StartRotateTimer();
    }

    protected override AnimatedSprite2D GetPrimarySprite() => _bodySprite;

    private void StartRotateTimer()
    {
        _rotateTimer.Start();
    }

    private void RotateRotator()
    {
        if (_rotator == null)
        {
            return;
        }

        if (_rotateTween != null && _rotateTween.IsValid())
        {
            _rotateTween.Kill();
        }

        // Reset rotation to 0 if we're at a multiple of 360 so we don't stack rotation in a weird way.
        if (_rotator.RotationDegrees % 360 == 0)
        {
            _rotator.RotationDegrees = 0;
        }

        float nextDegrees = _rotator.RotationDegrees + ROTATE_STEP;

        _rotateTween = _rotator
            .CreateTween()
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
        _rotateTween.TweenProperty(_rotator, "rotation_degrees", nextDegrees, ROTATE_DURATION);
        _rotateTween.TweenCallback(Callable.From(StartRotateTimer));
    }

    protected override void PlayFireAnimation()
    {
        _turretSprite.Play("fire");
    }

    protected override void OnProcessUpdate(double delta)
    {
        // Set the velocities of the deflectors
        _deflectorN.ConstantLinearVelocity = _currentVelocity;
        _deflectorS.ConstantLinearVelocity = _currentVelocity;

        SetMoveAnimation();
    }

    private void SetMoveAnimation()
    {
        if (_currentGlobalPosition != _lastGlobalPosition)
        {
            if (_bodySprite.Animation != "move" || !_bodySprite.IsPlaying())
            {
                _bodySprite.Play("move");
            }
        }
        else
        {
            if (_bodySprite.Animation != "default" || !_bodySprite.IsPlaying())
            {
                _bodySprite.Play("default");
            }
        }
    }

    protected override async Task PlayDeathSequence()
    {
        // Make the base and engine sprites invisible.
        _spriteContainer.Visible = false;

        // Remove collision on shields
        _deflectorN.ProcessMode = ProcessModeEnum.Disabled;
        _deflectorS.ProcessMode = ProcessModeEnum.Disabled;

        // Play the destruction sound
        _audioComponent.PlayDestructionSound();
    }
}
