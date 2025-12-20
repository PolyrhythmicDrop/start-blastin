using System;
using System.Linq;
using Autoloads;
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

        /// <summary>
        /// The target object to point the turret at if the selected <see cref="DynamicDirection"/> is TargetObject.
        /// </summary>
        public enum TargetObject
        {
            None,
            Nearest,
            LeastHealthy,
            StrongestAttack,
        }

        protected class TurretEffectState : EffectState
        {
            internal BarrelRack _turrets = new();

            internal TurretEffectState(Effect parent)
                : base(parent) { }

            public override void CleanUpState(GodotObject target)
            {
                if (target is not IWeaponOwner weaponOwner)
                {
                    return;
                }

                // Clean up any existing turrets in the rack.
                foreach (TurretBarrel turretBarrel in _turrets)
                {
                    if (turretBarrel != null && IsInstanceValid(turretBarrel))
                    {
                        turretBarrel.ToggleActive(turretBarrel.DefaultActive);
                        if (weaponOwner.Weapon.IsAncestorOf(turretBarrel) && !turretBarrel.Base)
                        {
                            weaponOwner.Weapon.Barrels.Remove(turretBarrel);
                            weaponOwner.Weapon.RemoveChild(turretBarrel);
                            turretBarrel.QueueFree();
                        }
                    }
                }
            }
        }

        private DynamicDirection _focusDirection;

        private bool _timedRotation = false;
        private float _rotateTime = 1.0f;

        private bool _objectTargeting = false;

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

        [ExportGroup("Object Targeting")]
        [Export(PropertyHint.GroupEnable)]
        public bool ObjectTargeting
        {
            get => _objectTargeting;
            set
            {
                _objectTargeting = value;
                OnObjectTargetingChanged();
            }
        }

        [Export]
        public TargetObject ObjectToTarget { get; set; }

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
                    if (_objectTargeting)
                    {
                        _objectTargeting = false;
                    }
                }
                else if (FocusDirection != DynamicDirection.TimedRotate && TimedRotation == true)
                {
                    _timedRotation = false;
                }

                if (FocusDirection == DynamicDirection.TargetObject && ObjectTargeting == false)
                {
                    _objectTargeting = true;
                    if (_timedRotation)
                    {
                        _timedRotation = false;
                    }
                }
                else if (FocusDirection != DynamicDirection.TargetObject && ObjectTargeting == true)
                {
                    _objectTargeting = false;
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
                if (_timedRotation == true && FocusDirection != DynamicDirection.TimedRotate)
                {
                    FocusDirection = DynamicDirection.TimedRotate;
                }
                else if (_timedRotation == false && FocusDirection == DynamicDirection.TimedRotate)
                {
                    FocusDirection = DynamicDirection.None;
                }
            }
        }

        private void OnObjectTargetingChanged()
        {
            if (Engine.IsEditorHint())
            {
                if (_objectTargeting == true && FocusDirection != DynamicDirection.TargetObject)
                {
                    FocusDirection = DynamicDirection.TargetObject;
                }
                else if (
                    _objectTargeting == false
                    && FocusDirection == DynamicDirection.TargetObject
                )
                {
                    FocusDirection = DynamicDirection.None;
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

            // Try to find an inactive turret in state to activate.
            if (turretState._turrets.Count > 0)
            {
                var inactive = turretState._turrets.GetBarrelsByActive(false).FirstOrDefault();
                // If you find one, activate the turret and return
                if (inactive is TurretBarrel inactiveTurret)
                {
                    inactiveTurret.ToggleActive(true);
                    return;
                }
            }

            // If there's nothing in the _turrets rack or you couldn't activate an inactive turret,
            // create a new one and add it to the rack.
            TurretBarrel turret = WeaponFactory.CreateTurretBarrel(
                weaponOwner,
                addToRack: true,
                activate: false,
                dynamicDirection: FocusDirection
            );

            if (_objectTargeting)
            {
                turret.SetTargetObjectType(ObjectToTarget);
            }
            else if (_focusDirection == DynamicDirection.TimedRotate)
            {
                turret.SetRotateDuration(_rotateTime);
            }

            // Set a random offset for each turret so they're not all stacked on top of one another.
            int offsetX = RNG.GetRandomInt(-10, 10);
            int offsetY = RNG.GetRandomInt(-10, 10);
            turret.Position = new Vector2(offsetX, offsetY);

            // Add the turret to the turret rack and activate it.
            turretState._turrets.Add(turret);
            turret.ToggleActive(true);
        }

        protected override void OnRemoveEffect(GodotObject target, EffectState state)
        {
            if (state is not TurretEffectState turretState)
            {
                return;
            }

            // Deactivate a turret from the state.
            if (turretState._turrets != null && turretState._turrets.Count > 0)
            {
                // Try to find an active barrel in the state. If we're removing the effect, at least one should be active.
                var active = turretState._turrets.GetBarrelsByActive(true).FirstOrDefault();
                if (active is TurretBarrel activeTurret)
                {
                    activeTurret.ToggleActive(false);
                    return;
                }
            }
        }
    }
}
