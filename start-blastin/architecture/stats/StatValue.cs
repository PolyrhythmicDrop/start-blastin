using System;
using Godot;

namespace Stats
{
    /// <summary>
    /// The value for a <see cref="Stat"/>.
    /// </summary>
    public partial class StatValue : RefCounted
    {
        private readonly object _value;

        /// <summary>
        /// Creates a new StatValue with the specified value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public StatValue(object value)
        {
            _value = value;
        }

        // Implicit conversions
        public static implicit operator StatValue(float value) => new(value);

        public static implicit operator StatValue(int value) => new(value);

        public static implicit operator StatValue(double value) => new(value);

        /// <summary>
        /// Gets the stat value as a specific type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="InvalidCastException"></exception>
        public T GetValue<T>()
            where T : IConvertible
        {
            if (_value == null)
            {
                throw new NullReferenceException($"{this}: _value is null!");
            }
            try
            {
                return (T)Convert.ChangeType(_value, typeof(T));
            }
            catch (Exception e)
            {
                GD.PrintErr(
                    $"{this}: Failed to convert {_value.GetType()} ({_value}) to {typeof(T)}: {e.Message}"
                );
                throw new InvalidCastException(
                    $"Cannot convert {_value.GetType()} to {typeof(T)}",
                    e
                );
            }
        }
    }
}
