using Godot;
using SafeResourcePicker;

namespace Weapons
{
    [GlobalClass]
    public partial class WeaponResource : Resource
    {
        private WeaponStats _stats;

        private string _scenePath;

        [Export]
        public WeaponStats Stats
        {
            get => _stats;
            set => _stats = value;
        }

        [Export]
        public string ScenePath
        {
            get => _scenePath;
            set => _scenePath = value;
        }
    }
}
