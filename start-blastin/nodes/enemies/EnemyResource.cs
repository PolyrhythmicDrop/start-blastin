using Components;
using Godot;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public partial class EnemyResource : Resource
    {
        protected string _scenePath;
        protected HealthComponent _healthComponent;
        protected WeaponResource _weaponResource;
        protected Curve2D _pathCurve;

        [Export]
        public string ScenePath
        {
            get => _scenePath;
            set => _scenePath = value;
        }

        [Export]
        public HealthComponent HealthComponent
        {
            get => _healthComponent;
            set => _healthComponent = value;
        }

        [Export]
        public WeaponResource WeaponResource
        {
            get => _weaponResource;
            set => _weaponResource = value;
        }

        /// <summary>
        /// The Curve2D describing the movement path of this enemy once it is spawned.
        /// This Curve2D should be applied to the enemy scene's Path2D node.
        /// </summary>
        [Export]
        public Curve2D PathCurve
        {
            get => _pathCurve;
            set => _pathCurve = value;
        }
    }
}
