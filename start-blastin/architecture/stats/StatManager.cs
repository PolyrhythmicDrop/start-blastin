using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Events;
using Godot;
using Utility;

namespace Stats
{
    /// <summary>
    /// Manages current stats for an entity using a Dictionary of <see cref="Stat"/> types.
    /// </summary>
    public partial class StatManager : GodotObject
    {
        private Dictionary<StatType, Stat> _stats = new();

        public IReadOnlyDictionary<StatType, Stat> Stats => _stats;

        public event EventHandler<StatUpdatedEventArgs> StatUpdated;

        public void RaiseStatUpdated(StatUpdatedEventArgs args)
        {
            StatUpdated?.Invoke(this, args);
        }

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
        public bool AddStat(Stat stat)
        {
            bool success = _stats.TryAdd(stat.Type, stat);
            if (success)
            {
                return success;
            }
            else
            {
                DebugLogger.LogMessage(
                    $"{stat.Type} already exists in this StatManager! Did you mean to use UpdateStat() instead?",
                    true
                );
                return success;
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
            float roundValue = MathF.Round(newValue, 4);

            if (stat != null)
            {
                stat.CurrentValue = roundValue;
            }
            else
            {
                AddStat(type, roundValue);
            }
            StatUpdatedEventArgs args = new(type, stat);
            RaiseStatUpdated(args);
        }

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
            StatUpdatedEventArgs args = new(stat.Type, stat);
            RaiseStatUpdated(args);
        }

        /// <summary>
        /// Retrieves the current Stat object of a specific type from the dictionary.
        /// </summary>
        /// <param name="type">The type of stat to retrieve.</param>
        /// <returns>
        /// The <see cref="Stat"/> of the same <paramref name="type"/> if it exists in the stats dictionary.
        /// Null if the passed StatType was not found.
        /// </returns>
        public Stat GetStat(StatType type)
        {
            bool success;
            if (HasStat(type))
            {
                try
                {
                    success = _stats.TryGetValue(type, out Stat stat);
                    if (success)
                    {
                        return stat;
                    }
                    else
                    {
                        throw new ArgumentNullException(
                            $"Stat of type {type} does not exist in StatManager dictionary."
                        );
                    }
                }
                catch (Exception e)
                {
                    DebugLogger.LogMessage(e.Message, true, true);
                    return null;
                }
            }
            else
            {
                DebugLogger.LogMessage($"Stat manager does not have a stat of type {type}!", true);
                return null;
            }
        }

        public bool HasStat(StatType type)
        {
            return _stats.ContainsKey(type);
        }
    }
}
