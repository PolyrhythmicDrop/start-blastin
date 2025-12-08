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
        Chain,
        None,
    }

    public enum Operation
    {
        Add,
        Multiply,
    }

    public enum Trigger
    {
        Equip,
        Chain,
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
            _targetExitCallable = Callable.From(
                (GodotObject target) =>
                {
                    OnTargetExitTree(target);
                }
            );
        }

        private void OnTargetExitTree(GodotObject target)
        {
            // Remove the target and effect state from the dictionary.
            if (target != null)
            {
                // Remove the state timer from the state.
                if (_targetStates.TryGetValue(target, out EffectState state))
                {
                    if (state.Timer != null)
                    {
                        state.Timer.Timeout -= () => OnEffectTimerTimeout(target);
                    }
                    // Remove the target from the list of effect states
                    _targetStates.Remove(target);
                }
            }
        }

        /// <summary>
        /// Retrieves the current state of the effect on a specified target.
        /// If the target doesn't have the effect active, adds a new EffectState to the <see cref="_targetStates"/> dictionary.
        /// </summary>
        /// <param name="target">The object whose effect state will be added or retrieved.</param>
        /// <returns>The current state of the effect on the <paramref name="target"/>.</returns>
        protected EffectState GetOrCreateEffectState(GodotObject target)
        {
            DebugLogger.LogMessage($"target: {target}", true);
            // If the targetStates dictionary doesn't contain the current target, add a new EffectState
            if (!_targetStates.ContainsKey(target))
            {
                _targetStates[target] = new EffectState(this);

                // Nullify the target if it leaves the scene
                if (target is Node node)
                {
                    if (!node.IsConnected(Node.SignalName.TreeExited, _targetExitCallable))
                    {
                        node?.Connect(Node.SignalName.TreeExited, _targetExitCallable);
                    }
                }
            }

            // Return the current state of the effect on the target.
            return _targetStates[target];
        }

        /// <summary>
        /// Returns this effect and any nested effects.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Effect> GetAllEffects()
        {
            yield return this;
        }

        private GodotObject GetTargetFromArgs(EventArgs args)
        {
            PlayerService playerService = ServiceManager.Instance.GetService<PlayerService>();
            return Target switch
            {
                TargetType.Self => args switch
                {
                    EnemyHitEventArgs enemyHit => playerService.GetPlayer(enemyHit.PlayerId),
                    EnemyKilledEventArgs enemyKilled => playerService.GetPlayer(
                        enemyKilled.PlayerId
                    ),
                    _ => null,
                },

                TargetType.Enemy => args switch
                {
                    EnemyHitEventArgs enemyHit => enemyHit.Enemy,
                    _ => null,
                },
                _ => null,
            };
        }

        /// <summary>
        /// Connects the effect timer's signals and starts the effect timer.
        /// Removes the EffectState when the timer goes off.
        /// </summary>
        /// <param name="state">The EffectState to remove when the timer goes off.</param>
        protected virtual void StartTimer(GodotObject target, EffectState state)
        {
            if (!_timed || target == null)
            {
                return;
            }
            if (target is Node node)
            {
                state.Timer = node?.GetTree().CreateTimer(_time, processAlways: false);
                state.Timer.Timeout += () => OnEffectTimerTimeout(target);
            }
        }

        protected virtual void OnEffectTimerTimeout(GodotObject target)
        {
            if (target != null)
            {
                RemoveEffectFromTarget(target);
            }
        }

        /// <summary>
        /// Applies the effect to a specific target. Used for manual application or for ChainEffects.
        /// </summary>
        /// <param name="target"></param>
        public void ApplyEffect(GodotObject target)
        {
            if (target == null)
            {
                return;
            }
            ApplyEffectToTarget(target);
        }

        /// <summary>
        /// Applies the effect to a target derived from event args. Used for event-based triggers.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public void ApplyEffect(object source, EventArgs args)
        {
            DebugLogger.LogMessage($"Applying effect using {args}!", true);
            GodotObject target = GetTargetFromArgs(args);
            if (target == null)
            {
                return;
            }
            ApplyEffectToTarget(target);
        }

        public void RemoveEffectStack(GodotObject target)
        {
            DebugLogger.LogMessage($"Removing effect stack from {target}!", true);
            if (target == null || !_targetStates.ContainsKey(target))
            {
                return;
            }
            RemoveEffectFromTarget(target);
        }

        public void RemoveAllEffectStacks(GodotObject target)
        {
            if (target == null || !_targetStates.ContainsKey(target))
            {
                return;
            }

            // Get the current state
            EffectState state = _targetStates[target];

            if (_stacking)
            {
                // Remove all stacks
                for (int i = state.CurrentStacks; i > 0; i--)
                {
                    RemoveEffectFromTarget(target);
                }
            }
            else
            {
                // Remove single effect
                RemoveEffectFromTarget(target);
            }
        }

        public void RemoveFromAllTargets()
        {
            // Create a copy of keys to avoid modifying collection during iteration
            List<GodotObject> targets = new(_targetStates.Keys);

            foreach (GodotObject target in targets)
            {
                RemoveAllEffectStacks(target);
            }
        }

        protected abstract void ApplyEffectToTarget(GodotObject target);

        protected abstract void RemoveEffectFromTarget(GodotObject target);
    }
}
