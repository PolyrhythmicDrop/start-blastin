using System;
using System.Threading.Tasks;
using Components;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class MiniVortex : EnemyNode
    {
        private AnimatedSprite2D _sprite;

        private const float SPIN_DURATION = 2;
        private const float SPIN_PROGRESS_RATIO = 0.2f;
        private const float SPIN_AMT_DEG = 1080;

        protected override void OnBaseReadyComplete()
        {
            _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        }

        public override AnimatedSprite2D GetPrimarySprite() => _sprite;

        public override void PlayFireAnimation()
        {
            _sprite.Play("fire");
        }

        protected override void FollowPath(float speed)
        {
            // Stop the fire timer until we get to our spinning fire position.
            if (_weaponComponent.IsFiring)
            {
                _weaponComponent.StopFiring();
            }

            float pathLength = _followPath.Curve.GetBakedLength();
            float totalDuration = MathF.Max(pathLength / speed, MIN_FOLLOW_TWEEN_DURATION);
            float stepDuration = MathF.Round(totalDuration / 5, 2);

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            _followTween = CreateTween();
            _followTween
                .TweenProperty(
                    _followPath.PathFollow,
                    "progress_ratio",
                    SPIN_PROGRESS_RATIO,
                    stepDuration
                )
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            ;
            _followTween.TweenCallback(
                Callable.From(() =>
                {
                    if (_alive)
                    {
                        _weaponComponent.StartFiring();
                    }
                })
            );
            _followTween
                .TweenProperty(this, "rotation_degrees", SPIN_AMT_DEG, SPIN_DURATION)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _followTween.TweenCallback(
                Callable.From(() =>
                {
                    if (_alive)
                    {
                        _weaponComponent.StopFiring();
                    }
                })
            );
            _followTween.TweenCallback(
                Callable.From(() =>
                {
                    if (_alive)
                    {
                        _sprite.Play("default");
                    }
                })
            );
            _followTween
                .TweenProperty(_followPath.PathFollow, "progress_ratio", 1.0f, stepDuration)
                .SetTrans(Tween.TransitionType.Circ)
                .SetEase(Tween.EaseType.In);
        }

        protected override async Task PlayDeathSequence()
        {
            _audioComponent.PlayDestructionSound();
            _sprite.Play("destruction");

            await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
            _sprite.Visible = false;
        }
    }
}
