using Godot;

namespace UI.HUD
{
    [GlobalClass]
    public partial class Hud : Control
    {
        private int _playerId;
        private StaticBody2D _hudBody;
        private CollisionShape2D _hudCollision;
        private PanelContainer _baseContainer;
        private WavePanel _wavePanel;
        private StatusPanel _statusPanel;

        public void Initialize(int playerId)
        {
            _playerId = playerId;
        }

        public override void _Ready()
        {
            _hudBody = GetNode<StaticBody2D>("%HUDBody");
            _hudCollision = GetNode<CollisionShape2D>("%HUDCollision");
            _baseContainer = GetNode<PanelContainer>("%BaseContainer");
            _wavePanel = GetNode<WavePanel>("%WavePanel");
            _statusPanel = GetNode<StatusPanel>("%StatusPanel");

            _statusPanel.Initialize(_playerId);

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            // Connect the shape re-sizer.
            Callable shapeResizeCallable = Callable.From(SetCollisionShape);
            if (!IsConnected(Control.SignalName.Resized, shapeResizeCallable))
            {
                Connect(Control.SignalName.Resized, shapeResizeCallable);
            }
        }

        private void SetCollisionShape()
        {
            RectangleShape2D newShape = new() { Size = Size };
            _hudCollision.Shape = newShape;
            _hudCollision.Position = Size / 2;
        }
    }
}
