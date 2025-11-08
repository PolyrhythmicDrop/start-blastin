using Autoloads;
using Godot;
using Utility;

namespace UI.Shop
{
    [GlobalClass]
    public partial class ShopManager : Node
    {
        private UiLayer _uiLayer;
        private int _playerId;
        private ShopUI _shopUI;
        private PackedScene _shopUiScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/ui/shop/shop-ui.tscn"
        );

        public ShopUI ShopUI => _shopUI;

        public override void _Ready()
        {
            // DebugLogger.LogMessage("Ready called!", true);
            _shopUI = _shopUiScene.Instantiate<ShopUI>();
            _shopUI.Initialize(_playerId);
            ConnectSignals();
        }

        public void Initialize(int playerId, UiLayer uiLayer)
        {
            DebugLogger.LogMessage($"Initializing shop base with player ID {playerId}", true);
            _playerId = playerId;
            _uiLayer = uiLayer;
        }

        private void ConnectSignals()
        {
            EventBus.Instance.WaveComplete += OpenShop;
            EventBus.Instance.StartWaveButtonPressed += CloseShop;
        }

        private void DisconnectSignals()
        {
            EventBus.Instance.WaveComplete -= OpenShop;
            EventBus.Instance.StartWaveButtonPressed -= CloseShop;
        }

        private async void OpenShop()
        {
            GD.Print($"Opening shop...");
            _uiLayer.CallDeferred(MethodName.AddChild, _shopUI);
            _shopUI.RequestReady();
            await ToSignal(_shopUI, Node.SignalName.Ready);
            _shopUI.Visible = true;
            EventBus.Instance.RaiseShopOpened();
        }

        private async void CloseShop()
        {
            GD.Print($"Closing shop...");
            _shopUI.Visible = false;
            // _uiLayer.ShopContainer.CallDeferred(MethodName.RemoveChild, _shopUI);
            _uiLayer.CallDeferred(MethodName.RemoveChild, _shopUI);
            await ToSignal(_shopUI, Node.SignalName.TreeExited);
            EventBus.Instance.RaiseShopClosed();
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
