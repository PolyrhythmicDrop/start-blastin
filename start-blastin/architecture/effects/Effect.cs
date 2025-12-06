using System;
using System.Runtime.InteropServices.Swift;
using System.Threading.Tasks;
using Entities;
using Events;
using Godot;
using Interfaces;
using Services;
using Stats;

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
        protected GodotObject _target;

        // ~~ Stacks and State ~~
        protected bool _active = false;
        protected bool _stacking = false;
        protected int _currentStacks = 0;
        protected int _maxStacks = 1;

        // ~~ Timed Stuff ~~

        protected bool _timed = false;
        protected float _time = 0.0f;
        protected SceneTreeTimer _timer;

        public bool Active => _active;

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

        public int CurrentStacks
        {
            get => _currentStacks;
            set => _currentStacks = Math.Min(_maxStacks, value);
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

        /// <summary>
        /// Set a target for the effect by passing in an object.
        /// </summary>
        /// <param name="target">The target for the effect.</param>
        public void SetTarget(GodotObject target)
        {
            _target = target;
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
            switch (Target)
            {
                case TargetType.Self:
                {
                    _target = args switch
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
                    _target = args switch
                    {
                        EnemyHitEventArgs enemyHit => enemyHit.Enemy,
                        _ => null,
                    };
                    break;
            }
        }

        public virtual void InitializeTimer() { }

        public virtual void ApplyEffect(object source, EventArgs args)
        {
            if (!_timed || _target == null)
            {
                return;
            }

            // Create a one-shot timer and start it.
            if (_target is Node node)
            {
                _timer = node.GetTree().CreateTimer(_time, processAlways: false);
                _timer.Timeout += RemoveEffect;
            }
        }

        public virtual void RemoveEffect() { }

        public virtual void RemoveAllEffectStacks() { }
    }
}
