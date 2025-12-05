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
    public abstract partial class Effect : Resource { }
}
