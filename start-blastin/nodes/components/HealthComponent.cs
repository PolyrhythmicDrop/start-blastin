using Godot;
using Interfaces;

namespace Components
{
    [GlobalClass]
    public partial class HealthComponent : Resource
    {
        private int _maxHealth;
        private int _currentHealth;
        private IDie _owner;

        [Export]
        public int MaxHealth
        {
            get => _maxHealth;
            set
            {
                if (value > 0)
                {
                    _maxHealth = value;
                }
                else
                {
                    _maxHealth = 1;
                }
            }
        }

        public int CurrentHealth
        {
            get => _currentHealth;
            private set
            {
                if (_currentHealth - value <= 0)
                {
                    _currentHealth = 0;
                    Die();
                }
                else
                {
                    _currentHealth = value;
                }
            }
        }

        public IDie Owner
        {
            get => _owner;
            set => _owner = value;
        }

        public void TakeDamage(int damage)
        {
            CurrentHealth -= damage;
        }

        public void Heal(int healAmount)
        {
            if (_currentHealth + healAmount >= _maxHealth)
            {
                CurrentHealth = _maxHealth;
            }
            else
            {
                CurrentHealth += healAmount;
            }
        }

        public void Die()
        {
            _owner.Die();
        }
    }
}
