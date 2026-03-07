using DataStructures;
using Godot;
using Utility;
using Weapons;

namespace Enemies
{
    [GlobalClass]
    public partial class EnemyResource : Resource
    {
        protected string _scenePath;
        protected float _maxHealth;
        protected WeaponStats _weaponStats;
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

        [Export(PropertyHint.Range, "1,100,1,or_greater")]
        public float MaxHealth
        {
            get => _maxHealth;
            set => _maxHealth = value;
        }

        [Export]
        public WeaponStats WeaponStats
        {
            get => _weaponStats;
            set
            {
                if (value == null)
                {
                    DebugLogger.LogMessage(
                        $"WeaponStats for {ResourceName} is being set to null!",
                        true,
                        true
                    );
                }
                _weaponStats = value;
            }
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

        [Export]
        public SoundSet Sounds { get; set; }
    }
}
