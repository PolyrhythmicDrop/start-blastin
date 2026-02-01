using Components;
using Entities;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class Drone : EnemyNode
    {
        private RayCast2D _visionRay;
        private Node2D _spriteContainer;
        private AnimatedSprite2D _base;
        private AnimatedSprite2D _engine;
        private AnimatedSprite2D _destruction;

        // ~~ Sound Strings ~~ //

        public override void _Ready()
        {
            base._Ready();
            // Stop the fire timer since we'll control it using the raycast instead.
            _weapon.FireTimer.Stop();

            _visionRay = GetNode<RayCast2D>("%VisionRay");
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _base = _spriteContainer.GetNode<AnimatedSprite2D>("%Base");
            _engine = _spriteContainer.GetNode<AnimatedSprite2D>("%Engine");
            _destruction = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");

            _currentGlobalPosition = GlobalPosition;
            _lastGlobalPosition = _currentGlobalPosition;

            SetHealthBarSize();

            FollowPath(_followSpeed);
        }

        protected override void SetHealthBarSize()
        {
            // Get size of the base sprite
            SpriteFrames sprite = _base.SpriteFrames ?? null;
            if (sprite != null)
            {
                Rect2I usedRect = sprite.GetFrameTexture("default", 0).GetImage().GetUsedRect();
                _healthBar.SetSizeAndOffset(usedRect.Size);
            }
        }

        public override void _Process(double delta)
        {
            // _lastGlobalPosition = _currentGlobalPosition;
            // _currentGlobalPosition = GlobalPosition;

            base._Process(delta);
            SetMoveAnimation();

            KinematicCollision2D collision = MoveAndCollide(_motion, true);

            if (collision != null)
            {
                OnCrash(collision);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (_visionRay.IsColliding() && _visionRay.GetCollider() is Player)
            {
                if (_weapon.FireTimer.IsStopped())
                {
                    FireWeapon();
                    _weapon.FireTimer.Start(_weapon.Stats.FireRate);
                }
            }
            else if (!_visionRay.IsColliding() && !_weapon.FireTimer.IsStopped())
            {
                _weapon.FireTimer.Stop();
            }
        }

        private void SetMoveAnimation()
        {
            if (_currentGlobalPosition != _lastGlobalPosition)
            {
                _engine.Play("moving");
            }
            else
            {
                _engine.Play("idle");
            }
        }

        protected override void FollowPath(float speed)
        {
            float pathLength = _followPath.Curve.GetBakedLength();
            float stepDuration = Mathf.Max((pathLength / speed) * 0.5f, MIN_FOLLOW_TWEEN_DURATION);

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            _followTween = CreateTween();
            _followTween
                .TweenProperty(_followPath, "FollowRatio", 0.5, stepDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _followTween.TweenProperty(_followPath, "FollowRatio", 1.0, stepDuration);
        }

        protected override void FireWeapon()
        {
            if (_alive)
            {
                base.FireWeapon();
                _base.Play("fire");
            }
        }

        public override void Die(int? playerId = null)
        {
            _alive = false;
            _weapon.FireTimer.Stop();
            _shape.Disabled = true;

            // Make the base and engine sprites invisible.
            _base.Visible = false;
            _engine.Visible = false;

            // Play the destruction sound
            _audioComponent.PlayDestructionSound();
            _destruction.Visible = true;
            _destruction.Play();

            if (
                !_destruction.IsConnected(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    Callable.From(() => base.Die(playerId))
                )
            )
            // Call the base "die" method when the animation is finished.
            {
                _destruction.Connect(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    Callable.From(() => base.Die(playerId))
                );
            }
        }
    }
}
