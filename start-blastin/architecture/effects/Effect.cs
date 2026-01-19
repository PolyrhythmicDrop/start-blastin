using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Autoloads;
using Enemies;
using Events;
using Godot;
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

    public enum EnemyTargetSubType
    {
        SingleEnemy,
        AllEnemies,
        ClosestEnemy,
        LeastHealthEnemy,
        StrongestEnemy,
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
        PlayerHitByProjectile,
        WhilePhasing,
        PhaseStart,
        PhaseEnd,
    }

    [Tool]
    [GlobalClass]
    public abstract partial class Effect : Resource
    {
        /// <summary>
        /// Manages state of a given effect on a specific target object.
        /// </summary>
        protected class EffectState
        {
            // Parent effect
            internal Effect _parent;
            protected int _currentStacks = 0;

            /// <summary>
            /// Whether or not the effect is currently active on the target.
            /// </summary>
            public bool Active { get; set; } = false;

            /// <summary>
            /// The amount of stacks of the effect currently active on the target.
            /// </summary>
            public int CurrentStacks
            {
                get => _currentStacks;
                set =>
                    _currentStacks =
                        _parent._maxStacks >= 0 ? Math.Min(_parent._maxStacks, value) : value;
            }

            /// <summary>
            /// List of Timers for each stack of the effect. Each stack is timed independently.
            /// </summary>
            public List<Timer> Timers { get; set; }

            /// <summary>
            /// Callback for the SceneTreeTimer timeout signal.
            /// </summary>
            internal Action _onTimerTimeout;

            /// <summary>
            /// Callback for when the target exits the tree.
            /// </summary>
            internal Action _onTreeExit;

            internal EffectState(Effect parent)
            {
                _parent = parent;
            }

            /// <summary>
            /// Class-specific cleanup methods. Performs any extra freeing or garbage collection required by the specific effect state.
            /// </summary>
            /// <param name="target">The target whose state to clean up.</param>
            public virtual void CleanUpState(GodotObject target) { }
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
        protected float _time = 0.1f;

        // ~~ Targeting ~~

        protected TargetType _targetType;
        protected bool _enemyTargeting = false;

        [Export]
        public Trigger Trigger { get; set; }

        [ExportGroup("Targeting")]
        [Export]
        public TargetType Target
        {
            get => _targetType;
            set
            {
                _targetType = value;
                OnTargetChanged();
            }
        }

        [ExportSubgroup("Enemy Targeting")]
        [Export(PropertyHint.GroupEnable)]
        public bool EnemyTargeting
        {
            get => _enemyTargeting;
            set
            {
                _enemyTargeting = value;
                OnEnemyTargetingChanged();
            }
        }

        [Export]
        public EnemyTargetSubType EnemyTarget { get; set; } = EnemyTargetSubType.SingleEnemy;

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

        #region Tool Methods

        protected virtual void OnTargetChanged()
        {
            if (Target == TargetType.Enemy)
            {
                _enemyTargeting = true;
            }
            else
            {
                _enemyTargeting = false;
            }
        }

        protected virtual void OnEnemyTargetingChanged()
        {
            if (EnemyTargeting == true && Target != TargetType.Enemy)
            {
                Target = TargetType.Enemy;
            }
            else if (EnemyTargeting == false && Target == TargetType.Enemy)
            {
                Target = TargetType.None;
            }
        }

        #endregion

        #region Enable / Disable

        /// <summary>
        /// Enables the effect based on the selected Target and Trigger.
        /// </summary>
        /// <remarks>
        /// Does *not* apply the effect unless the Trigger is set to Equip or Chain.
        /// Otherwise, application is still done via the selected Trigger.
        /// </remarks>
        public virtual void Enable(GodotObject target = null)
        {
            try
            {
                switch (Trigger)
                {
                    // Maybe work on this one, this doesn't feel quite right.
                    case Trigger.Equip:
                    {
                        // Apply the effect immediately if we've supplied a target, and the target type is self.
                        // Do not apply the effect if we're a StatEffect, since those are applied all together in the Player class.
                        if (target != null && Target == TargetType.Self && this is not StatEffect)
                        {
                            // Apply all stacks if stacking.
                            if (_stacking)
                            {
                                for (int i = 0; i < MaxStacks; i++)
                                {
                                    ApplyEffect(target);
                                }
                            }
                            else
                            {
                                ApplyEffect(target);
                            }
                        }
                        // Don't throw an exception if this is a StatEffect, since that's applied separately but is still handled.
                        else if (this is StatEffect)
                        {
                            return;
                        }
                        else
                        {
                            throw new ArgumentException(
                                $"Cannot apply a Trigger.Equip effect without a target, or without TargetType set to Self! Target: {target} | Target: {Target}",
                                paramName: nameof(target)
                            );
                        }
                        break;
                    }
                    // Triggers immediately if its parent chain effect is triggered.
                    case Trigger.Chain:
                        if (target != null)
                        {
                            ApplyEffect(target);
                        }
                        else
                        {
                            throw new ArgumentException(
                                $"Parent chain effect must supply a target to nested effects with Trigger set to Chain!"
                            );
                        }
                        break;
                    case Trigger.EnemyKilled:
                        EventBus.Instance.EnemyKilled += ApplyEffect;
                        break;
                    case Trigger.EnemyHit:
                        EventBus.Instance.EnemyHit += ApplyEffect;
                        break;
                    case Trigger.PlayerHitByProjectile:
                        EventBus.Instance.PlayerHitByProjectile += ApplyEffect;
                        break;
                    case Trigger.WhilePhasing:
                    case Trigger.PhaseStart:
                        EventBus.Instance.PhaseStarted += ApplyEffect;
                        break;
                    case Trigger.PhaseEnd:
                        EventBus.Instance.PhaseEnded += ApplyEffect;
                        break;
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        public virtual void Disable(GodotObject target = null)
        {
            // DebugLogger.LogMessage($"Calling Disable() on {target}...", true);
            try
            {
                switch (Trigger)
                {
                    case Trigger.Equip:
                    {
                        if (target != null)
                        {
                            ClearTargetEffectState(target);
                        }
                        else
                        {
                            ClearEffectStatesFromAllTargets();
                        }
                        break;
                    }
                    case Trigger.Chain:
                        if (target != null)
                        {
                            ClearTargetEffectState(target);
                        }
                        else
                        {
                            ClearEffectStatesFromAllTargets();
                        }
                        break;
                    case Trigger.EnemyKilled:
                        EventBus.Instance.EnemyKilled -= ApplyEffect;
                        ClearEffectStatesFromAllTargets();
                        break;
                    case Trigger.EnemyHit:
                        EventBus.Instance.EnemyHit -= ApplyEffect;
                        ClearEffectStatesFromAllTargets();
                        break;
                    case Trigger.PlayerHitByProjectile:
                        EventBus.Instance.PlayerHitByProjectile -= ApplyEffect;
                        ClearEffectStatesFromAllTargets();
                        break;
                    case Trigger.WhilePhasing:
                    case Trigger.PhaseStart:
                        EventBus.Instance.PhaseStarted -= ApplyEffect;
                        ClearEffectStatesFromAllTargets();
                        break;
                    case Trigger.PhaseEnd:
                        EventBus.Instance.PhaseEnded -= ApplyEffect;
                        ClearEffectStatesFromAllTargets();
                        break;
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        #endregion
        #region Targets and State

        /// <summary>
        /// Returns this effect and any nested effects.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Effect> GetAllEffects()
        {
            yield return this;
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
        /// Creates a new effect state.
        /// </summary>
        /// <returns></returns>
        protected virtual EffectState CreateEffectState()
        {
            return new EffectState(this);
        }

        /// <summary>
        /// Assigns the _onTreeExit callback and connects it to the node's TreeExiting signal.
        /// </summary>
        /// <param name="target"></param>
        protected void SetUpTargetExitHandler(GodotObject target)
        {
            if (target is Node node && _targetStates.TryGetValue(target, out EffectState state))
            {
                state._onTreeExit = () => OnTargetExitTree(node);
                node.TreeExiting += state._onTreeExit;
            }
        }

        /// <summary>
        /// Callback that is called when the passed <see cref="target"/> exits the tree.
        /// </summary>
        /// <param name="target"></param>
        private void OnTargetExitTree(GodotObject target)
        {
            // DebugLogger.LogMessage($"Target {target} exiting tree!");

            // Remove the target and effect state from the dictionary, if it exists
            if (target != null && _targetStates.TryGetValue(target, out EffectState state))
            {
                // Disconnect all the signals and free the timer
                DisconnectStateSignals(target, state);
            }
            _targetStates.Remove(target);
        }

        protected virtual void DisconnectStateSignals(GodotObject target, EffectState state)
        {
            // Clean up the tree exit callback
            if (target is Node node && state._onTreeExit != null)
            {
                node.TreeExiting -= state._onTreeExit;
            }

            // Clean up the state's timer connections
            if (state.Timers != null)
            {
                foreach (Timer fxTimer in state.Timers)
                {
                    if (state._onTimerTimeout != null)
                    {
                        DisconnectEffectTimer(target, fxTimer, state._onTimerTimeout);
                    }
                }
                state.Timers.Clear();
            }
        }

        protected virtual void DisconnectEffectTimer(
            GodotObject target,
            Timer timer,
            Action callback
        )
        {
            if (
                timer != null
                && callback != null
                && timer.IsConnected(Timer.SignalName.Timeout, Callable.From(callback))
            )
            {
                timer.Timeout -= callback;
            }

            // Remove and free the timer
            if (IsInstanceValid(timer))
            {
                if (target is Node parent && parent.IsAncestorOf(timer))
                {
                    parent.RemoveChild(timer);
                }
                timer.QueueFree();
            }
        }

        protected virtual void OnEffectTimerTimeout(GodotObject target)
        {
            // DebugLogger.LogMessage($"Effect timer timed out!");
            if (target != null && IsInstanceValid(target))
            {
                RemoveEffectFromTarget(target);
            }
        }

        /// <summary>
        /// Updates parameters of the event state post-application or post-removal.
        /// </summary>
        /// <param name="state">The EffectState to adjust.</param>
        /// <param name="postApplication">True to perform post-application adjustment. False to perform post-removal adjustment.</param>
        protected virtual void UpdateEffectState(
            GodotObject target,
            EffectState state,
            bool postApplication
        )
        {
            if (postApplication)
            {
                state.Active = true;
                if (_stacking)
                {
                    state.CurrentStacks++;
                }

                // Create and start the timer(s) for the target if necessary
                if (_timed)
                {
                    // Create a new Timers list if one doesn't currently exist on the state.
                    if (state.Timers == null)
                    {
                        state.Timers = new();
                        // if (_stacking)
                        // {
                        //     state.Timers.Capacity =
                        //         _maxStacks <= 0 ? state.CurrentStacks + 1 : _maxStacks;
                        // }
                    }
                    // Start the timer(s)
                    StartTimer(target, state);
                }
            }
            else
            {
                if (_stacking)
                {
                    state.CurrentStacks = Math.Max(0, state.CurrentStacks - 1);
                    if (state.CurrentStacks == 0)
                    {
                        state.Active = false;
                    }
                }
                else
                {
                    state.Active = false;
                }
            }
        }

        #endregion

        #region Application


        /// <summary>
        /// Applies the effect to a specific target. Used for manual application or for ChainEffects.
        /// Calls <see cref="ApplyEffectToTarget"/> with the passed target.
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
        /// Gets a target based on the passed EventArgs, then calls <see cref="ApplyEffectToTarget"/> with the new target.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public void ApplyEffect(object source, EventArgs args)
        {
            if (_enemyTargeting && EnemyTarget == EnemyTargetSubType.AllEnemies)
            {
                ApplyEffectToAllEnemies();
                return;
            }

            GodotObject target = GetTargetFromArgs(args);
            if (target == null)
            {
                return;
            }

            ApplyEffectToTarget(target);
        }

        protected void ApplyEffectToAllEnemies()
        {
            foreach (EnemyNode enemy in EnemyFinder.GetAllEnemies())
            {
                ApplyEffectToTarget(enemy);
            }
        }

        /// <summary>
        /// Gets a target from the passed <see cref="EventArgs"/>, based on the Effect's selected <see cref="TargetType"/> and <see cref="Trigger"/>
        /// </summary>
        /// <param name="args">EventArgs to select a target from.</param>
        /// <returns>A target for the Effect.</returns>
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
                    PlayerHitByProjectileEventArgs playerHitByProj => playerService.GetPlayer(
                        playerHitByProj.PlayerId
                    ),
                    PlayerIdEventArgs playerIdArgs => playerService.GetPlayer(
                        playerIdArgs.PlayerId
                    ),
                    _ => null,
                },

                TargetType.Enemy => EnemyTarget switch
                {
                    EnemyTargetSubType.SingleEnemy or EnemyTargetSubType.ClosestEnemy => args switch
                    {
                        // Target the enemy you hit.
                        EnemyHitEventArgs enemyHit
                            when EnemyTarget == EnemyTargetSubType.SingleEnemy => enemyHit.Enemy,
                        // Target the closest enemy to the enemy you hit. If there is no other enemy, return the enemy you hit.
                        EnemyHitEventArgs enemyHit
                            when EnemyTarget == EnemyTargetSubType.ClosestEnemy =>
                            EnemyFinder.GetClosestEnemy(enemyHit.Enemy.GlobalPosition, false)
                                ?? enemyHit.Enemy,
                        // Target the nearest enemy to the enemy killed
                        EnemyKilledEventArgs enemyKilled => EnemyFinder.GetClosestEnemy(
                            enemyKilled.KillPosition
                        ),
                        // Target the enemy that shot you. If the projectile was not owned by an enemy, set target to the nearest enemy to the player. If the enemy that shot the player is dead (and thus null), target the closest enemy to the player.
                        PlayerHitByProjectileEventArgs playerHitByProj => playerHitByProj
                            .Projectile
                            .SourceWeapon
                            .EnemyOwned
                            ? (EnemyNode)playerHitByProj.Projectile.SourceWeapon.WeaponOwner
                                ?? EnemyFinder.GetClosestEnemy(
                                    playerService.GetPlayer(playerHitByProj.PlayerId).GlobalPosition
                                )
                            : EnemyFinder.GetClosestEnemy(
                                playerService.GetPlayer(playerHitByProj.PlayerId).GlobalPosition
                            ),
                        // Target the nearest enemy to the player
                        PlayerIdEventArgs playerIdArgs => EnemyFinder.GetClosestEnemy(
                            playerService.GetPlayer(playerIdArgs.PlayerId).GlobalPosition
                        ),
                        _ => null,
                    },
                    // EnemyTargetSubType.AllEnemies => EnemyFinder.GetAllEnemies().FirstOrDefault(),
                    EnemyTargetSubType.LeastHealthEnemy => EnemyFinder.GetLeastHealthyEnemy(),
                    EnemyTargetSubType.StrongestEnemy => EnemyFinder.GetStrongestEnemy(),
                    _ => EnemyFinder.GetAllEnemies().FirstOrDefault(),
                },
                _ => null,
            };
        }

        /// <summary>
        /// Checks to see if we can apply the effect based on the passed <see cref="state"/> and the Effect's parameters (timing, stacking, etc.)
        /// </summary>
        /// <param name="state">The state of the effect to check against.</param>
        /// <returns>True if the effect can be applied, false if not.</returns>
        protected virtual bool CanApplyEffect(EffectState state)
        {
            // If stacking is disabled, the effect is not active on the target.
            bool stateInactive = !_stacking && !state.Active;

            // If stacking is enabled, check that current stacks are less than max stacks.
            bool stacksUnderThreshold =
                _stacking && state.CurrentStacks < _maxStacks && _maxStacks > 0;

            // If stacking is enabled and _maxStacks is set to less than 0, infinite stacks can be applied.
            bool infStacking = _stacking && _maxStacks < 0;

            // If any of the above are true, you can apply the effect.
            return stateInactive || stacksUnderThreshold || infStacking;
        }

        /// <summary>
        /// Applies the effect to a passed target. Conducts all checks, EffectState getting/creation, and post-application updates.
        /// </summary>
        /// <param name="target">The target to apply the effect to.</param>
        protected void ApplyEffectToTarget(GodotObject target)
        {
            // DebugLogger.LogMessage($"Applying effect {GetType().Name} to {target}!");
            // Get the current effect state for this target or create a new one
            EffectState state = GetOrCreateEffectState(target);

            // Check if we can apply the effect based on the effect's parameters and the state of the target.
            if (!CanApplyEffect(state))
            {
                return;
            }

            // If the effect uses the WhilePhasing trigger, also connect to the PhaseEnded event
            if (Trigger == Trigger.WhilePhasing)
            {
                EventBus.Instance.PhaseEnded += (source, args) =>
                {
                    RemoveEffectFromTarget(target);
                };
            }

            // Perform class-specific effect application logic
            OnApplyEffect(target, state);

            // Adjust the EffectState
            UpdateEffectState(target, state, true);
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
                // Add a new timer to the list if we need to and start it
                if (state.Timers.Count == 0 || state.Timers.Count < state.CurrentStacks)
                {
                    Timer fxTimer = new();
                    state.Timers.Add(fxTimer);
                    fxTimer.OneShot = true;
                    fxTimer.Name = $"Timer{GetType().Name}{node.Name}-{state.Timers.Count}";
                    state._onTimerTimeout = () => OnEffectTimerTimeout(target);
                    fxTimer.Timeout += state._onTimerTimeout;
                    node.AddChild(fxTimer);
                    fxTimer.Start(state._parent.Time);
                }
                else
                {
                    // Sort the timers by time remaining.
                    state.Timers.Sort(
                        delegate(Timer x, Timer y)
                        {
                            if (x.TimeLeft == y.TimeLeft)
                            {
                                return 0;
                            }
                            else if (x.TimeLeft > y.TimeLeft)
                            {
                                return 1;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                    );

                    // Start the timer with the least time remaining (the one at the top of the newly-sorted list)
                    state.Timers[0].Start(state._parent.Time);
                }
            }
        }

        /// <summary>
        /// Class-specific effect application logic. This method is overridden by derived classes to execute custom effects.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="state"></param>
        protected abstract void OnApplyEffect(GodotObject target, EffectState state);

        #endregion

        #region Removal

        protected void RemoveEffectFromTarget(GodotObject target)
        {
            // DebugLogger.LogMessage($"Removing {this.ResourceName} from {target}", true);

            // Get the current EffectState, if any.
            try
            {
                if (_targetStates.TryGetValue(target, out EffectState state))
                {
                    // Check if there's an active effect or stack to remove.
                    if (!CanRemoveEffect(state))
                    {
                        return;
                    }

                    // Remove the effect using class-specific logic.
                    OnRemoveEffect(target, state);

                    // Update the EffectState as necessary.
                    UpdateEffectState(target, state, false);
                }
                else
                {
                    throw new ArgumentException(
                        $"{target} not found in target states dictionary for {ResourceName}! Cannot remove non-existent effect.",
                        paramName: nameof(target)
                    );
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
            }
        }

        /// <summary>
        /// Checks to see if we can remove the effect based on the passed <see cref="state"/> and the Effect's parameters (timing, stacking, etc.)
        /// </summary>
        /// <param name="state">The state of the effect to check against.</param>
        /// <returns>True if the effect can be removed, false if not.</returns>
        protected virtual bool CanRemoveEffect(EffectState state)
        {
            // If stacking is disabled, the effect is active on the target.
            bool stateActive = !_stacking && state.Active;

            // If stacking is enabled, there is at least one stack on the target.
            bool activeStacks = _stacking && state.CurrentStacks > 0;

            // If infinite stacking is enabled, the effect is active on the target.
            bool infEffectActive =
                _stacking && _maxStacks < 0 && (state.Active || state.CurrentStacks > 0);

            return stateActive || activeStacks || infEffectActive;
        }

        /// <summary>
        /// Class-specific effect removal logic. This method is overridden by derived classes to remove custom effects.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="state"></param>
        protected abstract void OnRemoveEffect(GodotObject target, EffectState state);

        #endregion

        #region Freeing and Cleanup
        /// <summary>
        /// Clears the effect state, if any, for a passed target. Disconnects all state signals, then calls <see cref="EffectState.CleanUpState(GodotObject)"/> for any state-specific cleanup.
        /// Removes the target from the _targetStates dictionary.
        /// </summary>
        /// <param name="target">The target to remove all effects from.</param>
        public void ClearTargetEffectState(GodotObject target)
        {
            // DebugLogger.LogMessage($"Removing all effect stacks from {this.ResourceName}!", true);

            if (_targetStates.TryGetValue(target, out EffectState state))
            {
                if (_stacking && state.CurrentStacks > 0)
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

                // Clean up signals
                DisconnectStateSignals(target, state);

                // Free anything that needs freeing from the state.
                state?.CleanUpState(target);
            }
            // Remove target and effect state from the dictionary
            _targetStates.Remove(target);
        }

        /// <summary>
        /// Clears all effect states from all targets.
        /// </summary>
        public void ClearEffectStatesFromAllTargets()
        {
            List<GodotObject> targets = [.. _targetStates.Keys];

            foreach (GodotObject target in targets)
            {
                ClearTargetEffectState(target);
            }
        }

        #endregion
    }
}
