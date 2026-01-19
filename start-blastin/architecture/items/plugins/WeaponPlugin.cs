using System;
using DataStructures;
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

        [Export]
        public AudioData FireSound { get; set; }
    }
}
