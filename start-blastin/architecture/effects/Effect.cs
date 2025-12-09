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
        Remove,
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
            protected Effect _parent;
            protected int _currentStacks = 0;
            public bool Active { get; set; } = false;
            public int CurrentStacks
            {
                get => _currentStacks;
                set => _currentStacks = Math.Min(_parent._maxStacks, value);
            }
            public SceneTreeTimer Timer { get; set; }
            internal Action _timerTimeout;

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

        [Export(PropertyHint.Range, "-1,10,1,greater_than")]
        public int MaxStacks
        {
            get => _maxStacks;
            set => _maxStacks = Math.Max(-1, value);
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

        private void OnTargetExitTree(GodotObject target)
        {
            DebugLogger.LogMessage($"Target {target} exiting tree!");
            // Remove the target and effect state from the dictionary.
            if (target != null)
            {
                // Remove the state timer from the state.
                if (_targetStates.TryGetValue(target, out EffectState state))
                {
                    if (state?.Timer != null)
                    {
                        state.Timer.Timeout -= state._timerTimeout;
                    }
                    // Remove the target from the list of effect states
                    _targetStates.Remove(target);
                }
            }
        }

        /// <summary>
        /// Connects the TreeExiting signal of the target to the OnTargetExitTree callback.
        /// </summary>
        /// <param name="target"></param>
        protected void SetUpTargetExitHandler(GodotObject target)
        {
            if (target is Node node)
            {
                node.TreeExiting += () => OnTargetExitTree(node);
            }
        }

        protected virtual EffectState CreateEffectState()
        {
            return new EffectState(this);
        }

        /// <summary>
        /// Retrieves the current state of the effect on a specified target.
        /// If the target doesn't have the effect active, adds a new EffectState to the <see cref="_targetStates"/> dictionary.
        /// </summary>
        /// <param name="target">The object whose effect state will be added or retrieved.</param>
        /// <returns>The current state of the effect on the <paramref name="target"/>.</returns>
        protected virtual EffectState GetOrCreateEffectState(GodotObject target)
        {
            // If the targetStates dictionary doesn't contain the current target, create and add a new EffectState
            if (!_targetStates.ContainsKey(target))
            {
                _targetStates[target] = CreateEffectState();

                // Set up the exit handler
                SetUpTargetExitHandler(target);
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
                state._timerTimeout = () => OnEffectTimerTimeout(target);
                state.Timer.Timeout += state._timerTimeout;
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

        protected void ApplyEffectToTarget(GodotObject target)
        {
            // Get the current effect state for this target or create a new one
            EffectState state = GetOrCreateEffectState(target);

            // Don't apply the effect if the effect is not stacking and not active
            if (!_stacking && state.Active)
            {
                return;
            }

            // Don't apply the effect if the effect is stacking but we're at max stacks.
            // Also check if _maxStacks is greater than 0. You can set _maxStacks to -1 (or other negative number) for infinite stacking.
            if (_stacking && state.CurrentStacks >= _maxStacks && _maxStacks > 0)
            {
                return;
            }

            OnApplyEffect(target, state);

            // Adjust the EffectState
            state.Active = true;
            if (_stacking)
            {
                state.CurrentStacks++;
            }

            // Start the timer for the target if necessary
            if (_timed)
            {
                StartTimer(target, state);
            }
        }

        protected abstract void OnApplyEffect(GodotObject target, EffectState state);

        protected void RemoveEffectFromTarget(GodotObject target)
        {
            // Return immediately if there's no currently active effect on the target.
            if (!_targetStates.ContainsKey(target))
            {
                return;
            }

            // Get the current effect state of the target
            EffectState state = _targetStates[target];

            // Don't remove the effect if it's not active (if not stacking) or if there are no current stacks.
            if (!_stacking && !state.Active || _stacking && state.CurrentStacks == 0)
            {
                return;
            }

            OnRemoveEffect(target, state);

            if (_stacking)
            {
                state.CurrentStacks = Math.Max(0, state.CurrentStacks - 1);
            }

            if (_stacking && state.CurrentStacks <= 0 || !_stacking)
            {
                state.Active = false;
            }
        }

        protected abstract void OnRemoveEffect(GodotObject target, EffectState state);
    }
}
