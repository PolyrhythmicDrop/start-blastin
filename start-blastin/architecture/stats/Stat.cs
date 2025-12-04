using System;
using Godot;
using Utility;

namespace Stats
{
    [GlobalClass]
    public partial class Stat : Resource
    {
        private StatType _type;
        private float _baseValue = 0;
        private float _currentValue = 0;

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
            _currentValue = baseValue;
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
