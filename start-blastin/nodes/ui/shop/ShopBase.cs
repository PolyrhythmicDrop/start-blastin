using System.Reflection;
using System.Threading.Tasks;
using Autoloads;
using Godot;

public partial class ShopBase : CanvasLayer
{
    private ShopUI _shopUI;
    private PackedScene _shopUiScene = ResourceLoader.Load<PackedScene>(
        "res://nodes/ui/shop/shop-ui.tscn"
    );

    public override void _Ready()
    {
        GD.Print(
            $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Ready called!"
        );
        _shopUI = _shopUiScene.Instantiate<ShopUI>();
        ConnectSignals();
    }

    private void ConnectSignals()
    {
        GD.Print(
            $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Connecting signals..."
        );
        EventBus.Instance.WaveComplete += OpenShop;

        GD.Print(
            $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: waveCompleteCallable connected!"
        );

        EventBus.Instance.StartWaveButtonPressed += CloseShop;

        GD.Print(
            $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: startWaveCallable connected!"
        );
    }

    private async void OpenShop()
    {
        GD.Print($"Opening shop...");
        CallDeferred(MethodName.AddChild, _shopUI);
        // await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _shopUI.RequestReady();
        await ToSignal(_shopUI, Node.SignalName.Ready);
        GD.Print($"Making the shop UI visible...");
        _shopUI.Visible = true;
        GD.Print($"Shop UI visible? {_shopUI.Visible}");
    }

    private void CloseShop()
    {
        GD.Print($"Closing shop...");
        _shopUI.Visible = false;
        CallDeferred(MethodName.RemoveChild, _shopUI);
    }
}
