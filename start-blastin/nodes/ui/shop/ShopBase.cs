using System.Reflection;
using System.Threading.Tasks;
using Autoloads;
using Entities;
using Godot;
using Utility;

namespace Shop
{
    [GlobalClass]
    public partial class ShopBase : CanvasLayer
    {
        private int _playerId;
        private ShopUI _shopUI;
        private PackedScene _shopUiScene = ResourceLoader.Load<PackedScene>(
            "res://nodes/ui/shop/shop-ui.tscn"
        );

        public override void _Ready()
        {
            DebugLogger.LogMessage("Ready called!", true);
            _shopUI = _shopUiScene.Instantiate<ShopUI>();
            _shopUI.Initialize(_playerId);
            ConnectSignals();
        }

        public void Initialize(int playerId)
        {
            DebugLogger.LogMessage($"Initializing shop base with player ID {playerId}", true);
            _playerId = playerId;
            Layer = 2;
        }

        private void ConnectSignals()
        {
            DebugLogger.LogMessage("Connecting signals...", true);
            EventBus.Instance.WaveComplete += OpenShop;
            EventBus.Instance.StartWaveButtonPressed += CloseShop;
        }

        private async void OpenShop()
        {
            GD.Print($"Opening shop...");
            CallDeferred(MethodName.AddChild, _shopUI);
            _shopUI.RequestReady();
            await ToSignal(_shopUI, Node.SignalName.Ready);
            _shopUI.Visible = true;
            EventBus.Instance.EmitSignal(EventBus.SignalName.ShopOpened);
        }

        private async void CloseShop()
        {
            GD.Print($"Closing shop...");
            _shopUI.Visible = false;
            CallDeferred(MethodName.RemoveChild, _shopUI);
            await ToSignal(_shopUI, Node.SignalName.TreeExited);
            EventBus.Instance.EmitSignal(EventBus.SignalName.ShopClosed);
        }
    }
}
