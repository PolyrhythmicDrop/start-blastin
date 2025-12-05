using Entities;
using Godot;
using Interfaces;
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
    }
}
