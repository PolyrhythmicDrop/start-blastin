using Entities;
using Godot;
using Interfaces;
using Stats;

namespace Effects
{
    public enum TargetType
    {
        Player,
        Projectile,
        Weapon,
        Enemy,
    }

    public enum Operation
    {
        Add,
        Multiply,
    }

    [GlobalClass]
    public abstract partial class Effect : Resource
    {
        public virtual void Apply(TargetType targetType, GodotObject target)
        {
            if (targetType == TargetType.Player && target is IStats statful)
            {
                Apply(statful);
            }
        }

        public virtual void Apply(IStats statfulTarget) { }

        public virtual void Remove(TargetType targetType, GodotObject target)
        {
            if (target is IStats statful)
            {
                Remove(statful);
            }
        }

        public virtual void Remove(IStats statfulTarget) { }
    }
}
