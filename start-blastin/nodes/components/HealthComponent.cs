using Godot;
using Interfaces;

namespace Components
{
    [GlobalClass]
    public partial class HealthComponent : Resource
    {
        private float _maxHealth;
        private float _currentHealth;
        private IDie _owner;

        [Export]
        public float MaxHealth
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

        public float CurrentHealth
        {
            get => _currentHealth;
            private set => _currentHealth = value;
        }

        public IDie Owner
        {
            get => _owner;
            set => _owner = value;
        }

        public void Initialize(IDie owner)
        {
            _currentHealth = _maxHealth;
            _owner = owner;
        }

        public void TakeDamage(float damage)
        {
            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        public void Heal(float healAmount)
        {
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        }

        public void Die()
        {
            _owner.Die();
        }
    }
}
