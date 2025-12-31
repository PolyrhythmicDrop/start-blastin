using Godot;
using SafeResourcePicker;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponResource : Resource
    {
        private WeaponStats _stats;

        [Export]
        public WeaponStats Stats
        {
            get => _stats;
            set => _stats = value;
        }
    }
}
