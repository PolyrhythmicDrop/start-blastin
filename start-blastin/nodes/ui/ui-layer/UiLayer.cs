using System;
using Godot;
using Shop;

/// <summary>
/// UI CanvasLayer for a specific player. Manages all UI elements for that player.
/// </summary>
[GlobalClass]
public partial class UiLayer : CanvasLayer
{
    private int _playerId;
    private ShopManager _shopManager;

    public override void _Ready()
    {
        // DebugLogger.LogMessage("Ready called!", true);
        ConnectSignals();
    }

    public void Initialize(int playerId)
    {
        _playerId = playerId;
        Layer = 2;

        // Initialize the child shop manager.
        _shopManager = new();
        _shopManager.Initialize(_playerId);
        AddChild(_shopManager);
    }

    private void ConnectSignals() { }
}
