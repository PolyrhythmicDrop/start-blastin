using Godot;
using SafeResourcePicker;

namespace Items
{
    [GlobalClass]
    public partial class Plugin : Item
    {
        private bool _equipped = false;
        private bool _upgradeable = false;
        private int _level;
        private string _nextLevel;

        [ExportGroup("Upgrade Properties")]
        [Export(PropertyHint.GroupEnable)]
        public bool Upgradeable
        {
            get => _upgradeable;
            set => _upgradeable = value;
        }

        [Export]
        public int Level
        {
            get => _level;
            set => _level = value;
        }

        [Export(SRP_HINT.RESOURCE_PATH, "Plugin")]
        public string NextLevel
        {
            get => _nextLevel;
            set => _nextLevel = value;
        }
    }
}
