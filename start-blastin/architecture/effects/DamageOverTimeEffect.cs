using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Interfaces;
using Utility;

namespace Effects
{
    [GlobalClass]
    [Tool]
    public partial class DamageOverTimeEffect : Effect
    {
        protected class DoTEffectState : EffectState
        {
            internal List<Timer> _dmgTimers;
            internal bool _timersInitialized;
            internal Action _dmgCallback;

            internal DoTEffectState(DamageOverTimeEffect parent, int maxTimers)
                : base(parent)
            {
                // Set up timers.
                _dmgTimers = new() { Capacity = maxTimers };

                for (int i = 0; i < maxTimers; i++)
                {
                    Timer timer = new()
                    {
                        WaitTime = parent.Frequency,
                        OneShot = false,
                        Autostart = false,
                    };
                    _dmgTimers.Add(timer);
                }
            }

            public override void CleanUpState(GodotObject target)
            {
                if (target is not Node node)
                {
                    return;
                }

                foreach (Timer t in _dmgTimers)
                {
                    if (IsInstanceValid(node) && node.IsAncestorOf(t))
                    {
                        t.Stop();
                        node.RemoveChild(t);
                    }
                }
                _dmgTimers.Clear();
            }
        }

        private float _damagePerTick;
        private float _frequency;

        [Export(PropertyHint.Range, "0.1,10,0.5,greater_than")]
        public float DamagePerTick
        {
            get => _damagePerTick;
            set => _damagePerTick = value;
        }

        [Export(PropertyHint.Range, "0.05,5,0.1,greater_than")]
        public float Frequency
        {
            get => _frequency;
            set => _frequency = value;
        }

        protected override EffectState CreateEffectState()
        {
            int maxTimers = _stacking ? _maxStacks : 1;
            return new DoTEffectState(this, maxTimers);
        }

        protected override void DisconnectStateSignals(GodotObject target, EffectState state)
        {
            if (state is DoTEffectState dotState)
            {
                foreach (Timer t in dotState._dmgTimers)
                {
                    t.Timeout -= dotState._dmgCallback;
                }
            }
            base.DisconnectStateSignals(target, state);
        }

        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (state is not DoTEffectState dotState)
            {
                return;
            }

            if (target is not IHealthful healthful)
            {
                return;
            }

            // Initialize the state timers if they're not already initialized
            if (!dotState._timersInitialized)
            {
                DebugLogger.LogMessage($"Initializing timers for DoTEffect!");
                dotState._timersInitialized = true;
                dotState._dmgCallback = () => TriggerDamage(healthful);
                foreach (Timer t in dotState._dmgTimers)
                {
                    t.Timeout += dotState._dmgCallback;
                    if (healthful is Node node && !node.IsAncestorOf(t))
                    {
                        node.AddChild(t);
                    }
                }
            }

            // See if there are any timers currently stopped and start the first one you find.
            Timer stopped = dotState._dmgTimers.FirstOrDefault(t => t.IsStopped());
            if (stopped != null)
            {
                stopped.Start();
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            if (state is not DoTEffectState dotState)
            {
                return;
            }

            if (target is not IHealthful healthful)
            {
                return;
            }

            // Stop the first timer that's currently active.
            Timer running = dotState._dmgTimers.FirstOrDefault(t => !t.IsStopped());
            if (running != null)
            {
                running.Stop();
            }
        }

        protected void TriggerDamage(IHealthful healthful)
        {
            if (healthful is Node node && !node.IsQueuedForDeletion())
            {
                healthful.TakeDamage(_damagePerTick);
            }
        }
    }
}
