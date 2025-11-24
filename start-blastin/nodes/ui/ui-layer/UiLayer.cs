using System;
using System.Collections.Generic;
using Autoloads;
using Godot;
using Interfaces;
using UI.HUD;
using UI.Loadout;
using UI.Shop;

namespace UI
{
    /// <summary>
    /// UI CanvasLayer for a specific player. Manages all UI elements for that player.
    /// </summary>
    [GlobalClass]
    public partial class UiLayer : CanvasLayer, IListener
    {
        private static readonly Dictionary<int, UiLayer> _instances = new();
        private int _playerId;

        private ShopUI _shopUI;
        private PackedScene _shopUiScene = ResourceLoader.Load<PackedScene>("uid://buyrlvs8oy1lu");

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

            AddChild(_shopUI);
            AddChild(_hud);
            AddChild(_pluginScreen);

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.WaveComplete += OpenShop;
            EventBus.Instance.StartWaveButtonPressed += CloseShop;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.WaveComplete -= OpenShop;
            EventBus.Instance.StartWaveButtonPressed -= CloseShop;
        }

        public void Initialize(int playerId)
        {
            _playerId = playerId;
            Layer = 2;

            // Register the instance in the static dictionary for easy finding
            _instances[_playerId] = this;

            // Initialize the shop
            _shopUI = _shopUiScene.Instantiate<ShopUI>();
            _shopUI.Initialize(_playerId);
            _shopUI.Visible = false;

            // Initialize the HUD
            _hud = _hudScene.Instantiate<Hud>();
            _hud.Initialize(_playerId);

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

        private void OpenPluginScreen()
        {
            GetTree().Paused = true;
            _pluginScreen.ToggleActivate(true);

            // Deactivate the shop if it's open
            if (_shopUI.Active)
            {
                _shopUI.ToggleActivate(false);
            }
        }

        private void ClosePluginScreen()
        {
            _pluginScreen.ToggleActivate(false);
            GetTree().Paused = false;
            // Reactivate the shop if it's open.
            if (_shopUI.Visible && !_shopUI.Active)
            {
                _shopUI.ToggleActivate(true);
            }
        }

        private void OpenShop()
        {
            _shopUI.StockShop();
            _shopUI.Visible = true;
            _shopUI.ToggleActivate(true);
            EventBus.Instance.RaiseShopOpened();
        }

        private void CloseShop()
        {
            _shopUI.Visible = false;
            _shopUI.ToggleActivate(false);
            EventBus.Instance.RaiseShopClosed();
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            _instances.Remove(_playerId);
            base._ExitTree();
        }
    }
}
