using System;
using Godot;
using Projectiles;
using SafeResourcePicker;

namespace Items
{
    [GlobalClass]
    public partial class WeaponPlugin : Plugin
    {
        private ProjectileType _projectileType;

        [Export]
        public ProjectileType ProjectileType
        {
            get => _projectileType;
            set => _projectileType = value;
        }

        [Export(SRP_HINT.RESOURCE_PATH, "AudioStreamRandomizer")]
        public string FireSound { get; set; }
    }
}
