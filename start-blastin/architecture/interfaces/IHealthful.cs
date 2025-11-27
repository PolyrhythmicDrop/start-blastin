using System;
using Godot;

namespace Interfaces
{
    public interface IHealthful
    {
        void TakeDamage(float damage, int? playerId = null);
        void Heal(float healAmount);
    }
}
