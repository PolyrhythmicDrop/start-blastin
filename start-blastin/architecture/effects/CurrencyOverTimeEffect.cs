using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Godot;
using Interfaces;
using Utility;

namespace Effects
{
    [GlobalClass]
    [Tool]
    public partial class CurrencyOverTimeEffect : Effect
    {
        protected class CoTEffectState : EffectState
        {
            internal List<Timer> _cashTimers;
            internal bool _timersInitialized;
            internal Action _cashCallback;

            internal CoTEffectState(CurrencyOverTimeEffect parent, int maxTimers)
                : base(parent)
            {
                // Set up timers.
                _cashTimers = new() { Capacity = maxTimers };

                for (int i = 0; i < maxTimers; i++)
                {
                    Timer timer = new()
                    {
                        WaitTime = parent.Frequency,
                        OneShot = false,
                        Autostart = false,
                    };
                    _cashTimers.Add(timer);
                }
            }

            public override void CleanUpState(GodotObject target)
            {
                if (target is not Node node)
                {
                    return;
                }

                foreach (Timer t in _cashTimers)
                {
                    if (IsInstanceValid(node) && node.IsAncestorOf(t))
                    {
                        t.Stop();
                        node.RemoveChild(t);
                    }
                    t.QueueFree();
                }
                _cashTimers.Clear();
            }
        }

        private int _bytesPerTick = 0;
        private int _fluxPerTick = 0;
        private float _frequency = 1;

        /// <summary>
        /// The amount of bytes to add or remove per tick.
        /// </summary>
        [Export(PropertyHint.Range, "-100,100,1,or_greater,or_less")]
        public int BytesPerTick
        {
            get => _bytesPerTick;
            set => _bytesPerTick = value;
        }

        /// <summary>
        /// The amount of bytes to add or remove per tick.
        /// </summary>
        [Export(PropertyHint.Range, "-100,100,1,or_greater,or_less")]
        public int FluxPerTick
        {
            get => _fluxPerTick;
            set => _fluxPerTick = value;
        }

        [Export(PropertyHint.Range, "0.05,5,greater_than")]
        public float Frequency
        {
            get => _frequency;
            set => _frequency = value;
        }

        protected override void OnTargetChanged()
        {
            if (Target != TargetType.Self && Target != TargetType.Ally)
            {
                DebugLogger.LogMessage(
                    $"Cannot set target for {GetType().Name} to anything other than a {typeof(Player)} object!",
                    true,
                    true
                );
                Target = TargetType.Self;
            }
        }

        protected override void OnEnemyTargetingChanged()
        {
            if (EnemyTargeting == true)
            {
                DebugLogger.LogMessage(
                    $"Cannot target enemies with a {GetType().Name}!",
                    true,
                    true
                );
                EnemyTargeting = false;
            }
        }

        protected override EffectState CreateEffectState()
        {
            int maxTimers = _stacking ? _maxStacks : 1;
            return new CoTEffectState(this, maxTimers);
        }

        protected override void DisconnectStateSignals(GodotObject target, EffectState state)
        {
            if (state is CoTEffectState dotState)
            {
                foreach (Timer t in dotState._cashTimers)
                {
                    t.Timeout -= dotState._cashCallback;
                }
            }
            base.DisconnectStateSignals(target, state);
        }

        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (state is not CoTEffectState cotState)
            {
                return;
            }

            if (target is not Player player)
            {
                return;
            }

            // Initialize the state timers if they're not already initialized
            if (!cotState._timersInitialized)
            {
                cotState._timersInitialized = true;
                cotState._cashCallback = () => TriggerCurrencyOperation(player);
                foreach (Timer t in cotState._cashTimers)
                {
                    t.Timeout += cotState._cashCallback;
                    if (!player.IsAncestorOf(t))
                    {
                        player.AddChild(t);
                    }
                }
            }

            // See if there are any timers currently stopped and start the first one you find.
            Timer stopped = cotState._cashTimers.FirstOrDefault(t => t.IsStopped());
            if (stopped != null)
            {
                stopped.Start();
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            if (state is not CoTEffectState cotState)
            {
                return;
            }

            if (target is not Player player)
            {
                return;
            }

            // Stop the first timer that's currently active.
            Timer running = cotState._cashTimers.FirstOrDefault(t => !t.IsStopped());
            if (running != null)
            {
                running.Stop();
            }
        }

        protected void TriggerCurrencyOperation(Player player)
        {
            if (IsInstanceValid(player))
            {
                player.Bytes += _bytesPerTick;
                player.Flux += _fluxPerTick;
            }
        }
    }
}
