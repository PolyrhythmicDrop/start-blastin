using System.Collections.Generic;
using System.Reflection;
using Autoloads;
using FileIO;
using Godot;
using Items;
using Utility;

public partial class ShopUI : Control
{
    private List<ShopItemContainer> _itemContainers;
    private List<Item> _itemPool = new();
    private Button _nextWaveButton;
    private Button _rerollButton;

    public void LoadItemPool() =>
        PoolLoader.LoadResourcePool(_itemPool, "res://resources/items/", true);

    public override void _Ready()
    {
        DebugLogger.LogMessage($"Calling _Ready...", true);
        LoadItemPool();

        _nextWaveButton = GetNode<Button>("%NextWaveButton");
        _rerollButton = GetNode<Button>("%RerollButton");

        _itemContainers = new()
        {
            GetNode<ShopItemContainer>("%ShopItemContainer1"),
            GetNode<ShopItemContainer>("%ShopItemContainer2"),
            GetNode<ShopItemContainer>("%ShopItemContainer3"),
        };

        ConnectSignals();
        PopulateShopSlots();
        // Grab the focus to the first shop item.
        _itemContainers[0].CallDeferred(MethodName.GrabFocus);
    }

    private void ConnectSignals()
    {
        // Connect reroll button signal
        Callable rerollCallable = Callable.From(RerollShop);
        if (!_rerollButton.IsConnected(Button.SignalName.Pressed, rerollCallable))
        {
            _rerollButton.Connect(Button.SignalName.Pressed, rerollCallable);
            GD.Print($"Reroll button connected!");
        }

        // Connect next wave signal
        Callable wavePressedCallable = Callable.From(() =>
        {
            EventBus.Instance.EmitSignal(EventBus.SignalName.StartWaveButtonPressed);
        });
        if (!_nextWaveButton.IsConnected(Button.SignalName.Pressed, wavePressedCallable))
        {
            _nextWaveButton.Connect(Button.SignalName.Pressed, wavePressedCallable);
            GD.Print($"Next wave button connected!");
        }
    }

    private void PopulateShopSlots()
    {
        foreach (ShopItemContainer container in _itemContainers)
        {
            GD.Print("Retrieving item from pool...");
            Item item = GetItemFromPool();

            while (_itemContainers.Find(container => container.Item == item) != null)
            {
                item = GetItemFromPool();
            }

            container.SetItem(item);
        }
    }

    private Item GetItemFromPool()
    {
        if (_itemPool == null || _itemPool.Count == 0)
        {
            return null;
        }

        // Get total weight of all items
        int totalWeight = 0;
        foreach (Item item in _itemPool)
        {
            totalWeight += (int)item.Rarity;
        }

        int randomValue = GD.RandRange(0, totalWeight - 1);

        int currentWeight = 0;

        foreach (Item item in _itemPool)
        {
            currentWeight += (int)item.Rarity;
            if (randomValue < currentWeight)
            {
                GD.Print(
                    $"{MethodBase.GetCurrentMethod().Name}: Item {item.Name} retrieved from pool!"
                );
                return item;
            }
        }

        // Return null if we couldn't find a matching item in the _itemPool
        GD.PrintErr($"Could not load an item from the item pool!");
        return null;
    }

    private void ClearItemContainers()
    {
        foreach (ShopItemContainer container in _itemContainers)
        {
            container.ClearItem();
        }
    }

    private void RerollShop()
    {
        GD.Print($"Rerolling shop...");
        ClearItemContainers();
        PopulateShopSlots();
    }
}
