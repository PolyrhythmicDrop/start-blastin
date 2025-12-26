using System;
using Godot;
using Interfaces;

namespace Effects
{
    [GlobalClass]
    [Tool]
    public partial class DeflectEffect : Effect
    {
        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (target is not IDeflector deflector)
            {
                return;
            }

            deflector.DeflectActive = true;
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            if (target is not IDeflector deflector)
            {
                return;
            }
            deflector.DeflectActive = false;
        }
    }
}
