using System;
using System.Collections.Generic;
using Godot;

namespace Effects
{
    [GlobalClass]
    public partial class AuraChainEffect : ChainEffect
    {
        protected class AuraChainEffectState : EffectState
        {
            internal bool _initialized = false;
            internal List<EffectAura> _auras;

            internal AuraChainEffectState(AuraChainEffect parent)
                : base(parent)
            {
                _parent = parent;
            }

            public override void CleanUpState(GodotObject target)
            {
                // Clean up any existing aura scenes
                foreach (EffectAura aura in _auras)
                {
                    if (IsInstanceValid(aura) && target is Node node && node.IsAncestorOf(aura))
                    {
                        node.RemoveChild(aura);
                        aura.QueueFree();
                    }
                }
            }
        }

        private PackedScene _auraScene = GD.Load<PackedScene>("uid://t6wcme7sc7j7");

        [Export(PropertyHint.Range, "1,1000,10,or_greater")]
        public float AuraRadius { get; set; }

        protected override EffectState CreateEffectState()
        {
            return new AuraChainEffectState(this);
        }

        protected override bool IsChainConditionMet(GodotObject target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Activates the aura.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectState"></param>
        protected override void OnApplyEffect(GodotObject target, EffectState effectState)
        {
            if (effectState is not AuraChainEffectState auraState)
            {
                return;
            }

            // If the aura state scene hasn't been set up for this target yet, perform initialization.
            if (!auraState._initialized && target is Node node)
            {
                InitializeAuraScenes(node, auraState);
            }

            // Find the first disabled aura and enable it.
            EffectAura disabled = auraState._auras.Find(a =>
                a.ProcessMode == Node.ProcessModeEnum.Disabled
            );
            if (disabled != null)
            {
                disabled.ProcessMode = Node.ProcessModeEnum.Inherit;
                disabled.Visible = true;
            }
        }

        private void InitializeAuraScenes(Node target, AuraChainEffectState auraState)
        {
            auraState._initialized = true;

            auraState._auras = new();

            int totalI = _stacking ? _maxStacks : 1;
            for (int i = 0; i < totalI; i++)
            {
                EffectAura aura = _auraScene.Instantiate<EffectAura>();
                aura.EffectEnableCallback = EnableChainedEffects;
                aura.EffectDisableCallback = DisableChainedEffects;
                target.AddChild(aura);
                aura.Visible = false;
                aura.Name = $"{aura.GetParent().Name}-EffectAura{i}";
                aura.CircleShape.Radius = AuraRadius;
                auraState._auras.Add(aura);
            }
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState effectState)
        {
            if (effectState is not AuraChainEffectState auraState)
            {
                return;
            }

            // Find the first enabled aura and disable it.
            EffectAura enabled = auraState._auras.Find(a =>
                a.ProcessMode == Node.ProcessModeEnum.Inherit
            );
            if (enabled != null)
            {
                enabled.Visible = false;
                enabled.ProcessMode = Node.ProcessModeEnum.Disabled;
            }
        }
    }
}
