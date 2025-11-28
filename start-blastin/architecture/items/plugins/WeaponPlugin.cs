using System;
using Godot;
using Projectiles;

namespace Items
{
    [GlobalClass]
    public partial class WeaponPlugin : Plugin
    {
        private ProjectileType _projectileType;

        private float _projectileSpeed;

        [Export]
        public ProjectileType ProjectileType
        {
            get => _projectileType;
            set => _projectileType = value;
        }

        // [Export(PropertyHint.Range, "0,9999,100,greater_than")]
        // public float ProjectileSpeed
        // {
        //     get => _projectileSpeed;
        //     set => Math.Max(0, value);
        // }
    }
}
