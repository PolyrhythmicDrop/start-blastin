using System;
using Godot;
using Interfaces;

namespace Effects
{
    [GlobalClass]
    public partial class DeflectEffect : Effect
    {
        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (target is not IDeflect deflector)
            {
                return;
            }

            deflector.DeflectEnabled = true;
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            if (target is not IDeflect deflector)
            {
                return;
            }

            deflector.DeflectEnabled = false;
        }
    }
}
