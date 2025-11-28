using System;
using Godot;
using Projectiles;

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
    }
}
