using Godot;

namespace Weapons
{
    public partial class Blaster : WeaponNode
    {
        private Node2D _projSpawnPointNode;

        public override Vector2 ProjSpawnPoint
        {
            get => _projSpawnPointNode.GlobalPosition;
        }

        public override void _Ready()
        {
            base._Ready();
            _projSpawnPointNode = GetNode<Node2D>("%ProjSpawnPoint");
        }
    }
}
