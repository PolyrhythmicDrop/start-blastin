using System;
using System.Runtime.InteropServices.Swift;
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
    }

    [GlobalClass]
    public abstract partial class Effect : Resource
    {
        protected GodotObject _target;

        [Export]
        public TargetType Target { get; set; }

        [Export]
        public Trigger Trigger { get; set; }

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

        public virtual void ApplyEffect(object source, EventArgs args) { }

        public virtual void RemoveEffect() { }
    }
}
