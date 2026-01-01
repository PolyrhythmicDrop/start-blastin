using Entities;
using Godot;
using Interfaces;

namespace PlayerComponents
{
    [GlobalClass]
    public partial class InventoryComponent : Node, IPlayerComponent
    {
        private Player _player;

        public void Initialize(Player player)
        {
            _player = player;
        }
    }
}
