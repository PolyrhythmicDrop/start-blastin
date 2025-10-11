using System;
using Godot;

namespace Interfaces
{
    public interface IHealthful
    {
        void TakeDamage(int damage);
        void Heal(int healAmount);
    }
}
