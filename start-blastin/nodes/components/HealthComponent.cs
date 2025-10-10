using Godot;

namespace Components
{
    [GlobalClass]
    public partial class HealthComponent : Resource
    {
        private int _maxHealth;
        private int _currentHealth;

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

        public int CurrentHealth => _currentHealth;
    }
}
