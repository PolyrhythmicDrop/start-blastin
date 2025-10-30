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
        protected float _speed;
        protected int _crashDamage;
        protected int _fluxReward;
        protected int _byteReward;

        [Export]
        public string ScenePath
        {
            get => _scenePath;
            set => _scenePath = value;
        }

        [Export]
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        [Export]
        public int CrashDamage
        {
            get => _crashDamage;
            set => _crashDamage = value;
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

        [Export]
        public Curve2D PathCurve
        {
            get => _pathCurve;
            set => _pathCurve = value;
        }

        [Export]
        public int FluxReward
        {
            get => _fluxReward;
            set => _fluxReward = value;
        }

        [Export]
        public int ByteReward
        {
            get => _byteReward;
            set => _byteReward = value;
        }

        public EnemyResource()
            : this(0, null, null, "", 0, null, 0, 0) { }

        public EnemyResource(
            int crashDamage,
            HealthComponent health,
            Curve2D pathCurve,
            string scenePath,
            float speed,
            WeaponResource weaponResource,
            int flux,
            int bytes
        )
        {
            _crashDamage = crashDamage;
            _healthComponent = health;
            _pathCurve = pathCurve;
            _scenePath = scenePath;
            _speed = speed;
            _weaponResource = weaponResource;
            _fluxReward = flux;
            _byteReward = bytes;
        }
    }
}
