using System;
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

        FollowPath(_path, _followSpeed);
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
        int sign = Math.Sign(_currentVelocity.X);
        switch (sign)
        {
            // If we're moving left
            case -1:
                _engine.Play("strafe-left");
                break;
            // If we're moving right
            case 1:
                _engine.Play("strafe-left");
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
}
