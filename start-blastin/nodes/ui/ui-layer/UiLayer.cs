using System;
using Godot;
using UI.Shop;

namespace UI
{
    /// <summary>
    /// UI CanvasLayer for a specific player. Manages all UI elements for that player.
    /// </summary>
    [GlobalClass]
    public partial class UiLayer : CanvasLayer
    {
        private int _playerId;
        private ShopManager _shopManager;
        private Hud _hud;
        private PackedScene _hudScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/ui/hud/hud.tscn"
        );
        private Container _shopContainer;
        private Container _hudContainer;

        public Container ShopContainer => _shopContainer;

        public override void _Ready()
        {
            if (_playerId == 0)
            {
                Initialize(1);
            }
            _shopContainer = GetNode<Container>("%ShopContainer");
            _hudContainer = GetNode<Container>("%HUDContainer");

            AddChild(_shopManager);
            _hudContainer.AddChild(_hud);

            ConnectSignals();
        }

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            Layer = 2;

            // Initialize the child shop manager.
            _shopManager = new();
            _shopManager.Name = $"ShopManager {_playerId}";
            _shopManager.Initialize(_playerId, this);

            // Initialize the HUD
            _hud = _hudScene.Instantiate<Hud>();
            _hud.Initialize(_playerId);
        }

        private void ConnectSignals() { }
    }
}
