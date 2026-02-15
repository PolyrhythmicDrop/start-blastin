using System.Threading.Tasks;
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
        private AnimatedSprite2D _body;
        private AnimatedSprite2D _engine;
        private AnimatedSprite2D _destruction;

        protected override void OnBaseReadyComplete()
        {
            _visionRay = GetNode<RayCast2D>("%VisionRay");
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _body = _spriteContainer.GetNode<AnimatedSprite2D>("%Base");
            _engine = _spriteContainer.GetNode<AnimatedSprite2D>("%Engine");
            _destruction = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");
        }

        public override AnimatedSprite2D GetPrimarySprite() => _body;

        protected override void OnProcessUpdate(double delta)
        {
            SetMoveAnimation();
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            if (_visionRay.IsColliding() && _visionRay.GetCollider() is Player)
            {
                if (!_weaponComponent.IsFiring)
                {
                    _weaponComponent.StartFiring();
                }
            }
            else if (!_visionRay.IsColliding() && _weaponComponent.IsFiring)
            {
                _weaponComponent.StopFiring();
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

        public override void PlayFireAnimation()
        {
            _body.Play("fire");
        }

        protected override async Task PlayDeathSequence()
        {
            // Make the base and engine sprites invisible.
            _body.Visible = false;
            _engine.Visible = false;

            // Play the destruction sound and show the destruction sprite
            _audioComponent.PlayDestructionSound();
            _destruction.Visible = true;
            _destruction.Play();

            // Return after the destruction animation is complete.
            await ToSignal(_destruction, AnimatedSprite2D.SignalName.AnimationFinished);
        }
    }
}
