using System;
using Enemies;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class MiniVortex : EnemyNode
    {
        private AnimatedSprite2D _sprite;

        private const float SPIN_DURATION = 2;

        public override void _Ready()
        {
            base._Ready();
            _sprite = GetNode<AnimatedSprite2D>("%Sprite");

            _currentGlobalPosition = GlobalPosition;
            _lastGlobalPosition = _currentGlobalPosition;

            FollowPath(_path, _followSpeed);
        }

        public override void _Process(double delta)
        {
            _lastGlobalPosition = _currentGlobalPosition;
            _currentGlobalPosition = GlobalPosition;

            base._Process(delta);

            KinematicCollision2D collision = MoveAndCollide(_motion, true);

            if (collision != null)
            {
                OnCrash(collision);
            }
        }

        protected override void FireWeapon()
        {
            if (_alive)
            {
                base.FireWeapon();
                _sprite.Play("fire");
            }
        }

        protected override void FollowPath(EntityPath path, float speed)
        {
            // Stop the fire timer until we get to our spinning fire position.
            if (!_weapon.FireTimer.IsStopped())
            {
                _weapon.FireTimer.Stop();
            }

            float pathLength = path.Curve.GetBakedLength();
            float totalDuration = MathF.Max(pathLength / speed, 0.1f);
            float stepDuration = MathF.Round(totalDuration / 5, 2);

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            _followTween = CreateTween();
            _followTween
                .TweenProperty(path.PathFollow, "progress_ratio", 0.2f, stepDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            ;
            _followTween.TweenCallback(
                Callable.From(() =>
                {
                    if (_alive)
                    {
                        _weapon.FireTimer.Start();
                    }
                })
            );
            _followTween
                .TweenProperty(this, "rotation_degrees", 1080f, SPIN_DURATION)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            _followTween.TweenCallback(
                Callable.From(() =>
                {
                    if (_alive)
                    {
                        _weapon.FireTimer.Stop();
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
                .TweenProperty(path.PathFollow, "progress_ratio", 1.0f, stepDuration)
                .SetTrans(Tween.TransitionType.Circ)
                .SetEase(Tween.EaseType.In);
        }

        public override void Die(int? playerId = null)
        {
            _alive = false;
            _weapon.FireTimer.Stop();
            _shape.Disabled = true;

            _sprite.Play("destruction");

            _sprite.AnimationFinished += () =>
            {
                _sprite.Visible = false;
                base.Die(playerId);
            };
        }
    }
}
