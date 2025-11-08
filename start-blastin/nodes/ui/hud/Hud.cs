using Godot;
using Interfaces;
using Utility;

namespace UI.HUD
{
    [GlobalClass]
    public partial class Hud : PanelContainer, IListener
    {
        private int _playerId;
        private StaticBody2D _hudBody;
        private CollisionShape2D _hudCollision;
        private WavePanel _wavePanel;
        private StatusPanel _statusPanel;
        private CurrencyPanel _currencyPanel;
        private LoadoutPanel _loadoutPanel;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
        }

        public override void _Ready()
        {
            _hudBody = GetNode<StaticBody2D>("%HUDBody");
            _hudCollision = GetNode<CollisionShape2D>("%HUDCollision");
            _wavePanel = GetNode<WavePanel>("%WavePanel");
            _statusPanel = GetNode<StatusPanel>("%StatusPanel");
            _currencyPanel = GetNode<CurrencyPanel>("%CurrencyPanel");
            _loadoutPanel = GetNode<LoadoutPanel>("%LoadoutPanel");

            _statusPanel.Initialize(_playerId);
            _currencyPanel.Initialize(_playerId);
            _loadoutPanel.Initialize(_playerId);

            SetCollisionShape();

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            // Connect the shape re-sizer.
            Resized += SetCollisionShape;
        }

        public void DisconnectSignals()
        {
            Resized -= SetCollisionShape;
        }

        private void SetCollisionShape()
        {
            DebugLogger.LogMessage("Setting collision shape...", true);
            RectangleShape2D newShape = new() { Size = Size };
            Vector2 size = newShape.Size;
            _hudCollision.Shape = newShape;
            _hudCollision.Position = size / 2;
            DebugLogger.LogMessage(
                $"Collision shape and size set! New size: {size} | Collision position: {_hudCollision.Position}"
            );
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
