using System;
using Entities;
using Godot;

namespace Effects
{
    /// <summary>
    /// Effect that adds or removes currency when triggered.
    /// </summary>
    /// <remarks>
    /// Only adds or removes a set amount of currency. To modify the amount of currency received from a normal source (like killing an enemy or scrapping an item),
    /// use an IncomeModifierEffect instead.
    /// </remarks>
    [Tool]
    [GlobalClass]
    public partial class CurrencyEffect : Effect
    {
        [Export(PropertyHint.Range, "-1000,1000,1,or_greater,or_lesser")]
        public int Bytes { get; set; }

        [Export(PropertyHint.Range, "-1000,1000,1,or_greater,or_lesser")]
        public int Flux { get; set; }

        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (target is Player player)
            {
                player.Bytes += Bytes;
                player.Flux += Flux;
            }
        }

        /// <summary>
        /// Removing the effect does nothing, as any currency added or removed is lost forever :`(
        /// </summary>
        /// <param name="target"></param>
        /// <param name="state"></param>
        protected override void OnRemoveEffect(GodotObject target, EffectState state) { }
    }
}
