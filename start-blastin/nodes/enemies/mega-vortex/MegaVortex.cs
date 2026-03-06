using System;
using System.Threading.Tasks;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class MegaVortex : EnemyNode
    {
        private Node2D _spriteContainer;
        private AnimatedSprite2D _bodySprite;
        private AnimatedSprite2D _gunSprite;

        private Callable _startFireCallable;
        private Callable _preFireAnimCallable;
        private Callable _postFireAnimCallable;
        private Callable _stopFireCallable;

        /// <summary>
        /// The duration of the spin cycle.
        /// </summary>
        private const float SPIN_DURATION = 15f;

        /// <summary>
        ///  The number of total revolutions that occur during the spin cycle.
        /// </summary>
        private const float TOTAL_REVOLUTIONS = 2;

        /// <summary>
        /// The progress along the path at which the MegaVortex stops and spins.
        /// </summary>
        private const float SPIN_PROGRESS_RATIO = 0.2f;

        protected override void OnBaseReadyComplete()
        {
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _bodySprite = GetNode<AnimatedSprite2D>("%Body");
            _gunSprite = GetNode<AnimatedSprite2D>("%Guns");

            _startFireCallable = Callable.From(() =>
            {
                if (_alive)
                {
                    _weaponComponent.StartFiring();
                }
            });

            _preFireAnimCallable = Callable.From(PreFire);

            _postFireAnimCallable = Callable.From(PostFire);

            _stopFireCallable = Callable.From(() =>
            {
                if (_alive)
                {
                    _weaponComponent.StopFiring();
                }
            });
        }

        private async void PreFire()
        {
            if (_alive)
            {
                _bodySprite.Play("prefire");
                _gunSprite.Play("prefire");
                await ToSignal(_bodySprite, AnimatedSprite2D.SignalName.AnimationFinished);
                _weaponComponent.StartFiring();
            }
        }

        private async void PostFire()
        {
            if (_alive)
            {
                _weaponComponent.StopFiring();
                _bodySprite.PlayBackwards("prefire");
                _gunSprite.PlayBackwards("prefire");
                await ToSignal(_bodySprite, AnimatedSprite2D.SignalName.AnimationFinished);
                _bodySprite.Play("default");
                _gunSprite.Play("default");
            }
        }

        public override AnimatedSprite2D GetPrimarySprite() => _bodySprite;

        public override void PlayFireAnimation()
        {
            _bodySprite.Play("fire");
            _gunSprite.Play("fire");
        }

        protected override void FollowPath(float speed)
        {
            // Stop the fire timer until we get to our spinning fire position.
            if (_weaponComponent.IsFiring)
            {
                _weaponComponent.StopFiring();
            }

            // Calculate base tween variables
            float pathLength = _followPath.Curve.GetBakedLength();
            float totalDuration = MathF.Max(pathLength / speed, MIN_FOLLOW_TWEEN_DURATION);
            float stepDuration = MathF.Round(totalDuration / 5, 2);

            // Calculate spin subtween variables
            float totalRotation = TOTAL_REVOLUTIONS * Mathf.DegToRad(360f);

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            // Create the spin subtween
            var spinTween = CreateTween();
            spinTween
                .TweenProperty(this, "rotation", totalRotation, SPIN_DURATION)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);

            // Create the firing subtween
            var fireTween = CreateTween();
            fireTween.TweenCallback(_preFireAnimCallable);

            _followTween = CreateTween();

            // Go to the stopping point
            _followTween
                .TweenProperty(
                    _followPath.PathFollow,
                    "progress_ratio",
                    SPIN_PROGRESS_RATIO,
                    stepDuration
                )
                .SetTrans(Tween.TransitionType.Circ)
                .SetEase(Tween.EaseType.Out);
            // Start spinning while doing fire stuff.
            _followTween.TweenSubtween(spinTween);
            _followTween.SetParallel(true);
            _followTween.TweenSubtween(fireTween);
            // _followTween.Chain().TweenCallback(_stopFireCallable);
            _followTween.Chain().TweenCallback(_postFireAnimCallable);
            _followTween.SetParallel(false);
            _followTween
                .TweenProperty(_followPath.PathFollow, "progress_ratio", 1.0f, stepDuration)
                .SetTrans(Tween.TransitionType.Circ)
                .SetEase(Tween.EaseType.In);
        }

        protected override Task PlayDeathSequence()
        {
            throw new System.NotImplementedException();
        }
    }
}
