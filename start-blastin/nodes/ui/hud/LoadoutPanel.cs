using System;
using System.Collections.Generic;
using Events;
using Factories;
using Godot;
using Interfaces;
using Items;
using UI.Loadout;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class LoadoutPanel : PanelContainer
    {
        private int _playerId;

        private LoadoutDisplay _loadoutDisplay;

        private HBoxContainer _hBox;
        private bool _initialized;
        public HBoxContainer HBox => _hBox;
        public bool Initialized => _initialized;

        public override void _Ready()
        {
            DebugLogger.LogMessage($"Calling Ready...", true);
            _hBox = GetNode<HBoxContainer>("%LoadoutHBox");
        }

        public void Initialize(int playerId)
        {
            if (!_initialized)
            {
                DebugLogger.LogMessage($"Initializing...", true);
                _playerId = playerId;
                _loadoutDisplay = new();
                _loadoutDisplay.Initialize(UiLayer.GetUiLayer(_playerId).LoadoutManager);
                if (_hBox != null)
                {
                    _hBox.AddChild(_loadoutDisplay.WeapSlot);
                    foreach (ItemDisplay display in _loadoutDisplay.PluginDisplays)
                    {
                        _hBox.AddChild(display);
                    }
                }
                _initialized = true;
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
        }
    }
}
