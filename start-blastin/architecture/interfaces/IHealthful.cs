using System;
using Godot;

namespace Interfaces
{
    public interface IHealthful
    {
        void TakeDamage(float damage);
        void Heal(float healAmount);
    }
}
