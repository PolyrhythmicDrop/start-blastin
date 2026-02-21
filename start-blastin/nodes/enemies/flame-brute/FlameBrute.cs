using System;
using System.Threading.Tasks;
using Components;
using Godot;
using Utility;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public partial class FlameBrute : EnemyNode
    {
        private Node2D _spriteContainer;

        private AnimatedSprite2D _bodySprite;
        private AnimatedSprite2D _rArmSprite;
        private AnimatedSprite2D _lArmSprite;

        private Area2D _swoopArea;
        private Area2D _fireArea;

        // Are we turning to face a target?
        private bool _targeting = false;

        // The original global rotation when this Flame Brute was instantiated. Return to this rotation after the player exits the targeting sphere.
        private float _startRotation;
        private double _rotateLerpElapsed = 0;
        private Node2D _target;

        // Are we swooping after the player?
        private bool _swooping = false;
        private Vector2 _swoopTargetPoint;
        private Vector2 _swoopEndPoint;
        private Timer _swoopEnableDelay;
        private Tween _swoopTween;

        private const float RETURN_ROTATE_WEIGHT = 0.08f;
        private const float RETURN_EASE_CURVE = 2.5f;
        private const float TARGET_ROTATE_WEIGHT = 0.15f;
        private const float TARGET_EASE_CURVE = 3.5f;

        public override AnimatedSprite2D GetPrimarySprite() => _bodySprite;

        protected override void OnBaseReadyComplete()
        {
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");

            _bodySprite = _spriteContainer.GetNode<AnimatedSprite2D>("%BodySprite");
            _lArmSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%LArmSprite");
            _rArmSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%RArmSprite");

            _swoopArea = GetNode<Area2D>("%SwoopDetectArea");
            _fireArea = GetNode<Area2D>("%FireArea");

            _startRotation = GlobalRotation;
            _followPath.PathFollow.Rotates = false;

            _swoopArea.BodyEntered += OnBodyEnteredSwoopArea;
            _fireArea.BodyEntered += OnBodyEnteredFireArea;
            _fireArea.BodyExited += OnBodyExitedFireArea;

            _swoopEnableDelay = new()
            {
                OneShot = true,
                Autostart = false,
                WaitTime = 2,
            };

            _swoopEnableDelay.Timeout += () => _swoopArea.ProcessMode = ProcessModeEnum.Inherit;

            AddChild(_swoopEnableDelay);

            _swoopEnableDelay.Start();
        }

        private async void OnBodyEnteredSwoopArea(Node2D body)
        {
            if (_swooping)
            {
                return;
            }

            _swooping = true;

            _swoopEndPoint = Position;
            _swoopTargetPoint = ToLocal(body.GlobalPosition);

            await StartSwoop();
        }

        private async Task StartSwoop()
        {
            if (_followTween.IsValid() && _followTween.IsRunning())
            {
                _followTween.Pause();
            }

            if (_bodySprite.Animation != "fire")
            {
                _bodySprite.Play("fire");
            }

            // Disable the swoop detection area
            _swoopArea.SetDeferred(Node.PropertyName.ProcessMode, 4);
            DebugLogger.LogMessage($"Process mode: {_shape.ProcessMode}");

            SetSwoopNodes(out Path2D swoopPathNode, out PathFollow2D swoopFollowNode);

            // Wait to give the player time to respond.
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

            TweenSwoop(swoopPathNode, swoopFollowNode);
        }

        private void SetSwoopNodes(out Path2D swoopPathNode, out PathFollow2D swoopFollowNode)
        {
            // Create the curve
            Curve2D swoopCurve = new();
            swoopCurve.AddPoint(Position);
            swoopCurve.AddPoint(_swoopTargetPoint);
            swoopCurve.AddPoint(Position);

            // Create the pathing nodes.
            swoopPathNode = new() { Curve = swoopCurve };
            swoopFollowNode = new() { Rotates = false };
            RemoteTransform2D remote = new() { UpdateRotation = false };

            // Add all to scene tree
            // Use AddSibling for proper functioning of the remote transform
            AddSibling(swoopPathNode);
            swoopPathNode.AddChild(swoopFollowNode);
            swoopFollowNode.AddChild(remote);

            // Assign the FlameBrute to the remote
            remote.RemotePath = remote.GetPathTo(this, true);
        }

        private void TweenSwoop(Path2D swoopPath, PathFollow2D swoopFollow)
        {
            if (_swoopTween != null && _swoopTween.IsValid())
            {
                _swoopTween.Kill();
            }

            // Do the swoop!
            float swoopDur = MathF.Max(swoopPath.Curve.GetBakedLength() / (_followSpeed * 4), 0.1f);

            _swoopTween = CreateTween()
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Quad);
            _swoopTween.TweenProperty(swoopFollow, "progress_ratio", 1.0f, swoopDur);
            _swoopTween.TweenCallback(Callable.From(() => EndSwoop(swoopPath)));
        }

        private void EndSwoop(Path2D swoopPath)
        {
            swoopPath.QueueFree();

            if (_target == null)
            {
                _weaponComponent.CallDeferred(EnemyWeaponComponent.MethodName.StopFiring);
                _bodySprite.Play("default");
            }

            if (!_followTween.IsRunning())
            {
                _followTween.Play();
            }

            _swooping = false;

            _swoopEnableDelay.Start();
        }

        private void OnBodyEnteredFireArea(Node2D body)
        {
            // If we already have a target or our current target is the same body that just entered, don't do anything.
            if (_target == body || _target != null)
            {
                return;
            }

            if (!_weaponComponent.IsFiring)
            {
                // _weaponComponent.StartFiring();
                _weaponComponent.CallDeferred(EnemyWeaponComponent.MethodName.StartFiring);
            }

            _target = body;
            _rotateLerpElapsed = 0;
            _targeting = true;
        }

        private void OnBodyExitedFireArea(Node2D body)
        {
            if (body == _target)
            {
                _targeting = false;
                _target = null;
                _rotateLerpElapsed = 0;

                if (!_swooping)
                {
                    // _weaponComponent.StopFiring();
                    _weaponComponent.CallDeferred(EnemyWeaponComponent.MethodName.StopFiring);
                    _bodySprite.Play("default");
                }
            }
        }

        protected override void OnProcessUpdate(double delta)
        {
            // Rotation and targeting
            if (_targeting)
            {
                RotateToTarget(delta, _target.GlobalPosition);
            }
            else if (GlobalRotation != _startRotation)
            {
                RotateToStartingRotation(delta);
            }
        }

        private void RotateToTarget(double delta, Vector2 targetPoint)
        {
            float toAngle = GlobalPosition.AngleToPoint(targetPoint);

            GlobalRotation = Mathf.LerpAngle(
                GlobalRotation,
                toAngle,
                Mathf.Ease(TARGET_ROTATE_WEIGHT, TARGET_EASE_CURVE)
            );
        }

        private void RotateToStartingRotation(double delta)
        {
            GlobalRotation = Mathf.LerpAngle(
                GlobalRotation,
                _startRotation,
                Mathf.Ease(RETURN_ROTATE_WEIGHT, RETURN_EASE_CURVE)
            );
        }

        public override void PlayFireAnimation()
        {
            if (_bodySprite.Animation != "fire")
            {
                _bodySprite.Play("fire");
            }
        }

        protected override Task PlayDeathSequence()
        {
            throw new NotImplementedException();
        }
    }
}
