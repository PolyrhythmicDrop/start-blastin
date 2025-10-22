using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;

namespace Stats
{
    /// <summary>
    /// Manages current stats for an entity using a Dictionary of <see cref="Stat"/> types.
    /// </summary>
    public class StatManager
    {
        private Dictionary<StatType, Stat> _stats = new();

        public IReadOnlyDictionary<StatType, Stat> Stats => _stats;

        /// <summary>
        /// Adds a new <see cref="Stat"/> of the specified type to the StatManager.
        /// </summary>
        /// <param name="type">The type of stat to add.</param>
        /// <param name="baseValue">The value of the stat.</param>
        public void AddStat(StatType type, float baseValue)
        {
            Stat stat = new(type, baseValue);
            AddStat(stat);
        }

        /// <summary>
        /// Adds a new stat using an existing <see cref="Stat"/> object to the StatManager.
        /// </summary>
        /// <param name="stat">The Stat object to add.</param>
        public void AddStat(Stat stat)
        {
            bool success = _stats.TryAdd(stat.Type, stat);
            if (success)
            {
                GD.Print(
                    $"{MethodBase.GetCurrentMethod().Name}: New StatType added: {stat.Type}: base value = {stat.GetBaseValue()} | current value = {stat.GetCurrentValue()}"
                );
            }
            else
            {
                GD.PrintErr(
                    $"{stat.Type} already exists in this StatManager! Did you mean to use UpdateStat() instead?"
                );
            }
        }

        /// <summary>
        /// Updates the value of an existing stat in the StatManager.
        /// </summary>
        /// <param name="type">The type of Stat to update.</param>
        /// <param name="newValue">The new value of the stat.</param>
        public void UpdateStat(StatType type, float newValue)
        {
            Stat stat = GetStat(type);
            if (stat != null)
            {
                stat.CurrentValue = newValue;
            }
            else
            {
                AddStat(type, newValue);
            }
            // Stat stat = new(type, newValue);
            // UpdateStat(stat);
        }

        // /// <summary>
        // /// Updates the value of an existing stat in the StatManager using an existing Stat object.
        // /// </summary>
        // /// <param name="stat">The Stat object to use to update the StatManager.</param>
        // public void UpdateStat(Stat stat)
        // {
        //     _stats[stat.Type] = stat;
        //     GD.Print(
        //         $"{MethodBase.GetCurrentMethod().Name}: Stat {stat.Type} updated to {stat.CurrentValue}!"
        //     );
        // }

        /// <summary>
        /// Resets a stat to its base value.
        /// </summary>
        /// <param name="type"></param>
        public void ResetStat(StatType type)
        {
            Stat stat = _stats[type];
            if (stat != null)
            {
                stat.CurrentValue = stat.BaseValue;
            }
        }

        /// <summary>
        /// Retrieves the current Stat object of a specific type from the dictionary.
        /// </summary>
        /// <param name="type">The type of stat to retrieve.</param>
        /// <returns></returns>
        public Stat GetStat(StatType type)
        {
            bool success = _stats.TryGetValue(type, out Stat stat);
            if (success)
            {
                // GD.Print(
                //     $"Stat {stat} successfully retrieved! Type: {stat.Type} | Current value: {stat.CurrentValue}"
                // );
                return stat;
            }
            else
            {
                GD.PrintErr(
                    $"Stat of type {type} does not exist in StatManager dictionary. Returning null."
                );
                return null;
            }
        }
    }
}
