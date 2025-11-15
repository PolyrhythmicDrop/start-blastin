using Entities;
using Godot;
using Services;
using UI.HUD;

public partial class PluginScreen : PanelContainer
{
    private int _playerId;
    private PlayerService _service => ServiceManager.Instance.GetService<PlayerService>();
    private LoadoutPanel _loadoutPanel;

    public int PlayerId => _playerId;

    public bool Active;

    public void Initialize(int playerId)
    {
        _playerId = playerId;
        Visible = false;
    }

    public override void _Ready()
    {
        _loadoutPanel = GetNode<LoadoutPanel>("%LoadoutPanel");
        _loadoutPanel.Initialize(_playerId);
        Active = true;
    }

    public void BuildPluginScreen()
    {
        Player player = _service.GetPlayer(_playerId);
    }

    public override void _ExitTree()
    {
        Active = false;
        base._ExitTree();
    }
}
