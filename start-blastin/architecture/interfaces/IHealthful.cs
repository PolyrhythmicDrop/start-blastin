using System;
using Godot;

namespace Interfaces
{
    public interface IHealthful
    {
        float CurrentHealth { get; }
        void TakeDamage(float damage, int? playerId = null);
        void Heal(float healAmount);
    }
}
