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

        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        public void Heal(int healAmount)
        {
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
        }

        public void Die()
        {
            GD.Print(
                $"{System.Reflection.MethodBase.GetCurrentMethod().ReflectedType}.{System.Reflection.MethodBase.GetCurrentMethod().Name} called!"
            );
            _owner.Die();
        }
    }
}
