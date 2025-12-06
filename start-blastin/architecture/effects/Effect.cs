using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Swift;
using System.Threading.Tasks;
using Entities;
using Events;
using Godot;
using Interfaces;
using Services;
using Stats;
using Utility;

namespace Effects
{
    public enum TargetType
    {
        Self,
        Ally,
        Enemy,
    }

    public enum Operation
    {
        Add,
        Multiply,
    }

    public enum Trigger
    {
        Equip,
        EnemyKilled,
        EnemyHit,
    }

    [GlobalClass]
    public abstract partial class Effect : Resource
    {
        /// <summary>
        /// Manages state of a given effect on a specific target object.
        /// </summary>
        protected class EffectState
        {
            private Effect _parent;
            private int _currentStacks = 0;
            public bool Active { get; set; } = false;
            public int CurrentStacks
            {
                get => _currentStacks;
                set => _currentStacks = Math.Min(_parent._maxStacks, value);
            }
            public SceneTreeTimer Timer { get; set; }

            internal EffectState(Effect parent)
            {
                _parent = parent;
            }
        }

        /// <summary>
        /// Per-target effect state tracking. Key is the target, value is the state of the effect on the target.
        /// </summary>
        protected Dictionary<GodotObject, EffectState> _targetStates = new();

        protected GodotObject _target;

        // ~~ Stacking ~~
        protected bool _stacking = false;
        protected int _maxStacks = 1;

        // ~~ Timing ~~

        protected bool _timed = false;
        protected float _time = 0.0f;

        protected Callable _targetExitCallable;

        [Export]
        public TargetType Target { get; set; }

        [Export]
        public Trigger Trigger { get; set; }

        [ExportGroup("Stacking")]
        [Export(PropertyHint.GroupEnable)]
        public bool Stacking
        {
            get => _stacking;
            set => _stacking = value;
        }

        [Export(PropertyHint.Range, "1,10,1,greater_than")]
        public int MaxStacks
        {
            get => _maxStacks;
            set => _maxStacks = Math.Max(1, value);
        }

        [ExportGroup("Timing")]
        [Export(PropertyHint.GroupEnable)]
        public bool Timed
        {
            get => _timed;
            set => _timed = value;
        }

        [Export(PropertyHint.Range, "0.1,20,0.1,greater_than")]
        public float Time
        {
            get => _time;
            set { _time = Math.Max(0.1f, value); }
        }

        public Effect()
        {
            _targetExitCallable = Callable.From(() =>
            {
                // Remove the target and effect state from the dictionary.
                if (_target != null)
                {
                    _targetStates.Remove(_target);
                    _target = null;
                }
            });
        }

        /// <summary>
        /// Retrieves the current state of the effect on a specified target.
        /// If the target doesn't have the effect active, adds a new EffectState to the <see cref="_targetStates"/> dictionary.
        /// </summary>
        /// <param name="target">The object whose effect state will be added or retrieved.</param>
        /// <returns>The current state of the effect on the <paramref name="target"/>.</returns>
        protected EffectState GetOrCreateEffectState(GodotObject target)
        {
            // If the targetStates dictionary doesn't contain the current target, add a new EffectState
            if (!_targetStates.ContainsKey(target))
            {
                _targetStates[target] = new EffectState(this);
            }
            // Return the current state of the effect on the target.
            return _targetStates[target];
        }

        /// <summary>
        /// Set a target for the effect by passing in an object.
        /// </summary>
        /// <param name="target">The target for the effect.</param>
        public void SetTarget(GodotObject target)
        {
            if (_target != target)
            {
                _target = target;
            }

            // Nullify the target if it leaves the scene
            if (_target is Node node)
            {
                if (!node.IsConnected(Node.SignalName.TreeExited, _targetExitCallable))
                {
                    DebugLogger.LogMessage($"Connecting nullify signal on {node.Name}", true);
                    node.Connect(Node.SignalName.TreeExited, _targetExitCallable);
                }
            }
        }

        /// <summary>
        /// Set a target for the effect by matching TargetType with the appropriate event args.
        /// </summary>
        /// <param name="args">Event args passed by an event.</param>
        /// <remarks>
        /// TODO: Add event types as desired.
        /// </remarks>
        public virtual void SetTarget(EventArgs args)
        {
            PlayerService playerService = ServiceManager.Instance.GetService<PlayerService>();
            GodotObject newTarget = null;
            switch (Target)
            {
                case TargetType.Self:
                {
                    newTarget = args switch
                    {
                        EnemyHitEventArgs enemyHit => playerService.GetPlayer(enemyHit.PlayerId),
                        EnemyKilledEventArgs enemyKilled => playerService.GetPlayer(
                            enemyKilled.PlayerId
                        ),
                        _ => null,
                    };
                    break;
                }
                case TargetType.Enemy:
                    newTarget = args switch
                    {
                        EnemyHitEventArgs enemyHit => enemyHit.Enemy,
                        _ => null,
                    };
                    break;
            }

            // If the new target isn't the same as the current target, set the target.
            // This is to avoid duplicating the nullification signal
            if (newTarget != _target)
            {
                _target = newTarget;
            }
            else
            {
                return;
            }

            // Nullify the target if it leaves the scene
            if (_target is Node node)
            {
                if (!node.IsConnected(Node.SignalName.TreeExited, _targetExitCallable))
                {
                    DebugLogger.LogMessage($"Connecting nullify signal on {node.Name}", true);
                    node.Connect(Node.SignalName.TreeExited, _targetExitCallable);
                }
            }
        }

        /// <summary>
        /// Connects the effect timer's signals and starts the effect timer.
        /// Removes the EffectState when the timer goes off.
        /// </summary>
        /// <param name="state">The EffectState to remove when the timer goes off.</param>
        protected virtual void StartTimer(EffectState state)
        {
            if (!_timed || _target == null)
            {
                return;
            }
            if (_target is Node node)
            {
                state.Timer = node.GetTree().CreateTimer(_time, processAlways: false);
                state.Timer.Timeout += () =>
                {
                    if (_target != null)
                    {
                        DebugLogger.LogMessage(
                            $"SceneTreeTimer {state?.Timer} timed out! Removing effect from {node?.Name}.",
                            true
                        );
                        RemoveEffectFromTarget(_target);
                    }
                };
            }
        }

        public virtual void ApplyEffect(object source, EventArgs args)
        {
            if (!_timed || _target == null)
            {
                return;
            }
        }

        public virtual void RemoveEffect() { }

        protected virtual void RemoveEffectFromTarget(GodotObject target) { }

        public virtual void RemoveAllEffectStacks() { }
    }
}
