using Godot;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponNode : Node2D
    {
        private WeaponStats _stats;

        public WeaponStats Stats => _stats;

        public void InitializeStats(WeaponStats stats)
        {
            _stats = stats;
        }
    }
}
