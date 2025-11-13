using System;
using System.Collections.Generic;
using Entities;
using Godot;
using UI.HUD;
using UI.Shop;

namespace UI
{
    /// <summary>
    /// UI CanvasLayer for a specific player. Manages all UI elements for that player.
    /// </summary>
    [GlobalClass]
    public partial class UiLayer : CanvasLayer
    {
        private static readonly Dictionary<int, UiLayer> _instances = new();
        private int _playerId;
        private ShopManager _shopManager;
        private Hud _hud;
        private PackedScene _hudScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/ui/hud/hud.tscn"
        );

        public int PlayerId => _playerId;

        public static UiLayer GetUiLayer(int playerId)
        {
            return _instances.TryGetValue(playerId, out UiLayer ui) ? ui : null;
        }

        public override void _Ready()
        {
            if (_playerId == 0)
            {
                Initialize(1);
            }

            AddChild(_shopManager);
            AddChild(_hud);
        }

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            Layer = 2;

            // Register the instance in the static dictionary for easy finding
            _instances[_playerId] = this;

            // Initialize the child shop manager.
            _shopManager = new();
            _shopManager.Name = $"ShopManager {_playerId}";
            _shopManager.Initialize(_playerId, this);

            // Initialize the HUD
            _hud = _hudScene.Instantiate<Hud>();
            _hud.Initialize(_playerId);
        }

        public override void _ExitTree()
        {
            _instances.Remove(_playerId);
            base._ExitTree();
        }
    }
}
