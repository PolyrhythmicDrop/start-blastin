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
        private PackedScene _hudScene = ResourceLoader.Load<PackedScene>("uid://cs0msq3g3i6xk");

        private PluginScreen _pluginScreen;
        private PackedScene _pluginScreenScene = ResourceLoader.Load<PackedScene>(
            "uid://dog71b3n5wml5"
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

            // Initialize the plugin screen
            _pluginScreen = _pluginScreenScene.Instantiate<PluginScreen>();
            _pluginScreen.Initialize(_playerId);
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressedByEvent("plugin-menu", @event))
            {
                if (_pluginScreen.Active)
                {
                    ClosePluginScreen();
                }
                else
                {
                    OpenPluginScreen();
                }
            }
        }

        private async void OpenPluginScreen()
        {
            GetTree().Paused = true;
            CallDeferred(MethodName.AddChild, _pluginScreen);
            _pluginScreen.RequestReady();
            await ToSignal(_pluginScreen, Node.SignalName.Ready);
            _pluginScreen.BuildPluginScreen();
            _pluginScreen.Visible = true;
        }

        private async void ClosePluginScreen()
        {
            _pluginScreen.Visible = false;
            CallDeferred(MethodName.RemoveChild, _pluginScreen);
            await ToSignal(_pluginScreen, Node.SignalName.TreeExited);
            GetTree().Paused = false;
        }

        public override void _ExitTree()
        {
            _instances.Remove(_playerId);
            base._ExitTree();
        }
    }
}
