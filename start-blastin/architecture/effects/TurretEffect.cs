using System;
using DataStructures;
using Effects;
using Factories;
using Godot;
using Interfaces;
using Utility;
using Weapons;

namespace Effects
{
    [Tool]
    [GlobalClass]
    public partial class TurretEffect : Effect
    {
        // The direction the barrel "looks" at. Generally a moving target of some kind.
        public enum DynamicDirection
        {
            // The barrel points in the default direction, aka North
            None,

            // The direction of the owner's movement.
            Movement,

            // Opposite the direction of the owner's movement.
            MovementOpposite,

            // An object in global coordinates.
            TargetObject,

            // The turret rotates 360deg over a set period of time.
            TimedRotate,
        }

        protected class TurretEffectState : EffectState
        {
            internal BarrelRack _turrets = new();
            internal Timer _rotateTimer;

            internal TurretEffectState(Effect parent)
                : base(parent) { }

            public override void CleanUpState(GodotObject target) { }
        }

        private DynamicDirection _focusDirection;

        private bool _timedRotation = false;

        private float _rotateTime = 1.0f;

        [Export]
        public DynamicDirection FocusDirection
        {
            get => _focusDirection;
            set
            {
                _focusDirection = value;
                OnFocusDirectionChanged();
            }
        }

        [ExportGroup("Timed Rotation")]
        [Export(PropertyHint.GroupEnable)]
        public bool TimedRotation
        {
            get => _timedRotation;
            set
            {
                _timedRotation = value;
                OnTimedRotationChanged();
            }
        }

        [Export(PropertyHint.Range, "0.1,10,0.1,greater_than")]
        public float RotateTime
        {
            get => _rotateTime;
            set => _rotateTime = value;
        }

        /// <summary>
        /// Automatically changes the TimedRotation boolean in the editor based on the new value of FocusDirection.
        /// </summary>
        private void OnFocusDirectionChanged()
        {
            if (Engine.IsEditorHint())
            {
                if (FocusDirection == DynamicDirection.TimedRotate && TimedRotation == false)
                {
                    _timedRotation = true;
                }
                else if (FocusDirection != DynamicDirection.TimedRotate && TimedRotation == true)
                {
                    _timedRotation = false;
                }
            }
        }

        /// <summary>
        /// Automatically changes the FocusDirection selection in the editor based on the new value of TimedRotate.
        /// </summary>
        private void OnTimedRotationChanged()
        {
            if (Engine.IsEditorHint())
            {
                DebugLogger.LogMessage($"Timed rotation changed to {_timedRotation}", true);
                if (_timedRotation == true && FocusDirection != DynamicDirection.TimedRotate)
                {
                    _focusDirection = DynamicDirection.TimedRotate;
                }
                else if (_timedRotation == false && FocusDirection == DynamicDirection.TimedRotate)
                {
                    _focusDirection = DynamicDirection.None;
                }
            }
        }

        protected override EffectState CreateEffectState()
        {
            return new TurretEffectState(this);
        }

        protected override void OnApplyEffect(GodotObject target, EffectState state)
        {
            if (state is not TurretEffectState turretState)
            {
                return;
            }

            if (target is not IWeaponOwner weaponOwner)
            {
                return;
            }

            // If the state does not have a turret rack, initialize it.
            if (turretState._turrets == null)
            {
                turretState._turrets = new();
            }

            // Create a new turret.
            TurretBarrel turret = WeaponFactory.CreateTurretBarrel(
                weaponOwner,
                addToRack: true,
                activate: false,
                dynamicDirection: FocusDirection
            );

            turretState._turrets.Add(turret);
            turret.ToggleActive(true);
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state) { }
    }
}
