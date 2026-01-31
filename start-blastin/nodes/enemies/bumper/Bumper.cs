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

    private StaticBody2D _shieldN;
    private StaticBody2D _shieldS;

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

    public override void _Ready()
    {
        base._Ready();

        // Set all the child nodes
        _spriteContainer = GetNode<Node2D>("%SpriteContainer");
        _bodySprite = _spriteContainer.GetNode<AnimatedSprite2D>("%BodySprite");
        _rotator = _spriteContainer.GetNode<Node2D>("%Rotator");
        _turretSprite = _rotator.GetNode<AnimatedSprite2D>("%TurretSprite");
        _shieldGenSprite = _rotator.GetNode<AnimatedSprite2D>("%ShieldGenSprite");
        _shieldN = _shieldGenSprite.GetNode<StaticDeflector>("%DeflectorN");
        _shieldS = _shieldGenSprite.GetNode<StaticDeflector>("%DeflectorS");

        InitializeTimers();

        SetHealthBarSize();

        FollowPath(_followSpeed);
    }

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

    protected override void SetHealthBarSize()
    {
        // Get size of the base sprite
        SpriteFrames sprite = _bodySprite.SpriteFrames ?? null;
        if (sprite != null)
        {
            Rect2I usedRect = sprite.GetFrameTexture("default", 0).GetImage().GetUsedRect();
            _healthBar.SetSizeAndOffset(usedRect.Size);
        }
    }

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

    protected override void FireWeapon()
    {
        _turretSprite.Play("fire");
        base.FireWeapon();
    }

    public override void _Process(double delta)
    {
        _lastGlobalPosition = _currentGlobalPosition;
        _currentGlobalPosition = GlobalPosition;

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
        if (_currentGlobalPosition != _lastGlobalPosition)
        {
            _bodySprite.Play("move");
        }
        else
        {
            _bodySprite.Play("default");
        }
    }

    public override void Die(int? playerId = null)
    {
        _alive = false;
        _weapon.FireTimer.Stop();
        _shape.Disabled = true;

        // Make the base and engine sprites invisible.
        _spriteContainer.Visible = false;

        // Play the destruction sound
        _audioComponent.PlayDestructionSound();

        base.Die();
    }
}
