using System;
using System.Diagnostics;
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
        private CollisionPolygon2D _swoopPoly;
        private Area2D _fireArea;

        private bool _patrolStarted = false;
        private Tween _patrolTween;

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

        private const float NINETY_DEG_RAD = 1.5708f;

        private const float SWOOP_DELAY_TIME = 2;
        private const float RETURN_ROTATE_WEIGHT = 0.08f;
        private const float RETURN_EASE_CURVE = 2.5f;
        private const float TARGET_ROTATE_WEIGHT = 0.15f;
        private const float TARGET_EASE_CURVE = 3.5f;

        public override AnimatedSprite2D GetPrimarySprite() => _bodySprite;

        #region EnemyNode Overrides
        protected override void OnBaseReadyComplete()
        {
            // Set the sprite nodes
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _bodySprite = _spriteContainer.GetNode<AnimatedSprite2D>("%BodySprite");
            _lArmSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%LArmSprite");
            _rArmSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%RArmSprite");

            // Set the area and collision shape nodes
            _swoopArea = GetNode<Area2D>("%SwoopDetectArea");
            _swoopPoly = GetNode<CollisionPolygon2D>("%SwoopDetectPolygon");
            _fireArea = GetNode<Area2D>("%FireArea");

            // Set the initial curve and path settings
            _startRotation = GlobalRotation;
            _followPath.Curve = SetEnterCurve();
            _followPath.PathFollow.Rotates = false;

            // Connect area signals
            _swoopArea.BodyEntered += OnBodyEnteredSwoopArea;
            _fireArea.BodyEntered += OnBodyEnteredFireArea;
            _fireArea.BodyExited += OnBodyExitedFireArea;

            // Set the swoop delay timer
            SetSwoopDelay();
        }

        /// <summary>
        /// Called after Ready to start tweening the enter curve created with <see cref="SetEnterCurve"/>.
        /// </summary>
        /// <param name="speed"></param>
        protected override void FollowPath(float speed)
        {
            float pathLength = _followPath.Curve.GetBakedLength();
            float duration = Mathf.Max(pathLength / speed, MIN_FOLLOW_TWEEN_DURATION);

            if (_followTween != null)
            {
                _followTween.Kill();
            }

            _followTween = CreateTween();
            _followTween.TweenProperty(_followPath, "FollowRatio", 0.98f, duration);
            // _followTween.TweenCallback(Callable.From(StartPatrol));

            _followTween.Finished += async () =>
            {
                await StartPatrol();
            };
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

        #endregion


        #region Swoop
        /// <summary>
        /// Sets up the swoop delay timer, including adding it to the scene tree, connecting the timeout signal, adding the timer to the tree, and starting the timer.
        /// </summary>
        private void SetSwoopDelay()
        {
            _swoopEnableDelay = new()
            {
                OneShot = true,
                Autostart = false,
                WaitTime = SWOOP_DELAY_TIME,
            };

            AddChild(_swoopEnableDelay);

            _swoopEnableDelay.Timeout += () =>
            {
                _swoopPoly.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, false);
            };

            _swoopEnableDelay.Start();
        }

        /// <summary>
        /// Callback for when the player enters the swoop area.
        /// </summary>
        /// <param name="body"></param>
        private async void OnBodyEnteredSwoopArea(Node2D body)
        {
            if (_swooping)
            {
                return;
            }

            _swooping = true;

            _swoopEndPoint = GlobalPosition;
            _swoopTargetPoint = body.GlobalPosition;

            await StartSwoop();
        }

        /// <summary>
        /// Creates the swooping nodes and the swoop path. Calls <see cref="TweenSwoop"/> .
        /// </summary>
        private async Task StartSwoop()
        {
            if (_patrolTween != null && _patrolTween.IsValid() && _patrolTween.IsRunning())
            {
                _patrolTween.Pause();
            }

            if (_bodySprite.Animation != "fire")
            {
                _bodySprite.Play("fire");
            }

            // Disable the swoop detection area
            _swoopPoly.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, true);

            SetSwoopNodes(out Path2D swoopPathNode, out PathFollow2D swoopFollowNode);

            // Wait a moment to give the player time to respond.
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

            TweenSwoop(swoopPathNode, swoopFollowNode);
        }

        /// <summary>
        /// Creates the <see cref="Curve2D"/> for the swoop using the enemy's current position and the swoop target position.
        /// Also creates new <see cref="Path2D"/>, <see cref="PathFollow2D"/>, and <see cref="RemoteTransform2D"/> nodes and adds them all to the scene tree.
        /// Adds this enemy as the path for the RemoteTransform2D note.
        /// </summary>
        /// <param name="swoopPathNode"></param>
        /// <param name="swoopFollowNode"></param>
        private void SetSwoopNodes(out Path2D swoopPathNode, out PathFollow2D swoopFollowNode)
        {
            Node2D parent = GetParent<Node2D>();

            // Create the curve
            Curve2D swoopCurve = new();
            swoopCurve.AddPoint(parent.ToLocal(GlobalPosition));
            swoopCurve.AddPoint(parent.ToLocal(_swoopTargetPoint));
            swoopCurve.AddPoint(parent.ToLocal(_swoopEndPoint));

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

        /// <summary>
        /// Runs the swoop tween based on the passed Path2D and PathFollow2D nodes.
        /// </summary>
        /// <param name="swoopPath"></param>
        /// <param name="swoopFollow"></param>
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

        /// <summary>
        /// Ends the swoop animation and restarts the swoop delay timer.
        /// </summary>
        /// <param name="swoopPath"></param>
        private void EndSwoop(Path2D swoopPath)
        {
            swoopPath.QueueFree();

            if (_target == null)
            {
                _weaponComponent.CallDeferred(EnemyWeaponComponent.MethodName.StopFiring);
                _bodySprite.Play("default");
            }
            _swooping = false;

            if (!_patrolTween.IsRunning())
            {
                _patrolTween.Play();
            }

            _swoopEnableDelay.Start();
        }

        #endregion

        #region Area Callbacks
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

        #endregion

        #region Patrol

        /// <summary>
        /// Creates the initial Curve2D.
        /// </summary>
        /// <returns>A Curve2D from the spawner to the patrol point.</returns>
        private Curve2D SetEnterCurve()
        {
            Curve2D c = new() { BakeInterval = 100 };
            float startLength = MathF.Round(GD.RandRange(200, 500), 0);
            Vector2 endPoint = new(0, startLength);
            c.AddPoint(Vector2.Zero);
            c.AddPoint(endPoint);

            return c;
        }

        /// <summary>
        /// Starts the patrol behavior, including setting the <see cref="_patrolStarted"/> boolean and calling <see cref="SetPatrolCurve"/> to create the patrol curve.
        /// </summary>
        /// <returns></returns>
        private async Task StartPatrol()
        {
            if (!_patrolStarted)
            {
                if (_followTween != null && _followTween.IsValid())
                {
                    _followTween.Kill();
                }

                _patrolStarted = true;
                _followPath.GlobalPosition = GlobalPosition;

                _followPath.Curve = await SetPatrolCurve();
                _followPath.FollowRatio = UtilityMethods.GetCurveProgressRatio(
                    _followPath.Curve,
                    Position
                );

                FollowPatrolPath(_followSpeed);
            }
        }

        /// <summary>
        /// Creates and returns the patrol curve based on the enemy's starting rotation and spawner position.
        /// </summary>
        /// <returns>A <see cref="Curve2D"/> for patrolling.</returns>
        private async Task<Curve2D> SetPatrolCurve()
        {
            Vector2 viewSize = GetViewportRect().Size;

            // Get posmod for the starting rotation in degrees (like modulo, but "wraps around" for negative numbers)
            float normDeg = Mathf.PosMod(Mathf.RadToDeg(_startRotation), 360f);
            // Round to nearest 90 degree interval
            float startDeg = MathF.Round(normDeg / 90f) * 90f % 360f;
            // Convert back to radians to use in Godot methods
            float startRad = Mathf.DegToRad(startDeg);

            Curve2D c = new() { BakeInterval = 50 };

            // Create edge points based on starting rotation (basically, which side of the screen the enemy spawns from and the enemy's orientation), then convert those to local space for use in the curve.
            Vector2 edgePoint1 = startDeg switch
            {
                90 or 270 => ToLocal(new(viewSize.X - 200, GlobalPosition.Y)).Rotated(startRad),
                0 or 180 => ToLocal(new(GlobalPosition.X, viewSize.Y - 200))
                    .Rotated(NINETY_DEG_RAD),
                _ => ToLocal(new(600, 600)).Rotated(startRad),
            };

            Vector2 edgePoint2 = startDeg switch
            {
                90 or 270 => ToLocal(new(200, GlobalPosition.Y)).Rotated(startRad),
                0 or 180 => ToLocal(new(GlobalPosition.X, 200)).Rotated(NINETY_DEG_RAD),
                _ => ToLocal(new(600, 600)).Rotated(startRad),
            };

            // Add the points to the curve.
            c.AddPoint(edgePoint1);
            c.AddPoint(edgePoint2);

            return c;
        }

        /// <summary>
        /// Tweens the follow ratio of the patrol path created using <see cref="SetPatrolCurve"/>,
        /// Loops after reaching a certain point.
        /// </summary>
        /// <param name="speed"></param>
        protected void FollowPatrolPath(float speed)
        {
            if (_patrolStarted)
            {
                if (_patrolTween != null && _patrolTween.IsRunning())
                {
                    _patrolTween.Kill();
                }

                // Get the total path length
                float pathLength = _followPath.Curve.GetBakedLength();

                // Get the base duration using the total path length
                float baseDuration = MathF.Max(pathLength / speed, MIN_FOLLOW_TWEEN_DURATION);

                float initRatio = _followPath.PathFollow.ProgressRatio;
                // Get the remaining length using the current follow ratio so we can calucate the initial duration
                float remainingLength = pathLength - (initRatio * pathLength);

                // Calculate the duration for the remaining length.
                float initDuration = MathF.Max(remainingLength / speed, MIN_FOLLOW_TWEEN_DURATION);

                // Create the looping subtween
                var loopSubTween = CreateTween()
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine)
                    .SetLoops();
                loopSubTween.TweenProperty(_followPath, "FollowRatio", 0, baseDuration);
                loopSubTween.TweenProperty(_followPath, "FollowRatio", 0.95f, baseDuration);

                // Create the tween
                _patrolTween = CreateTween()
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Sine);
                // Set the initial movement before we start the loop.
                _patrolTween
                    .TweenProperty(_followPath, "FollowRatio", 0.95f, initDuration)
                    .FromCurrent();
                _patrolTween.TweenSubtween(loopSubTween);
            }
        }

        #endregion


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
    }
}
