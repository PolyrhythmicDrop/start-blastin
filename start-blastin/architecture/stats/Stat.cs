using System;
using Godot;

namespace Stats
{
    [GlobalClass]
    public partial class Stat : Resource
    {
        private StatType _type;
        private float _baseValue;
        private float _currentValue;

        [Export]
        public StatType Type
        {
            get => _type;
            set => _type = value;
        }

        [Export]
        public float BaseValue
        {
            get => _baseValue;
            set => _baseValue = value;
        }

        public float CurrentValue
        {
            get => _currentValue;
            set => _currentValue = value;
        }

        public Stat(StatType type, float baseValue)
        {
            _type = type;
            _baseValue = baseValue;
            _currentValue = _baseValue;
        }

        public float GetCurrentValue()
        {
            return _currentValue;
        }

        public float GetBaseValue()
        {
            return _baseValue;
        }
    }
}
