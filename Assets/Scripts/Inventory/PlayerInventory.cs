using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public List<ItemInstance> items = new List<ItemInstance>();

    public event System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("PlayerInventory Initialized");
    }

    public void AddItem(ItemSO itemSO, int quantity = 1)
    {
        if (itemSO == null)
        {
            Debug.LogError("AddItem called with null ItemSO!");
            return;
        }

        Debug.Log($"AddItem called: {quantity}x {itemSO.itemName}");

        // Try stacking
        if (itemSO.canStack)
        {
            foreach (var existing in items)
            {
                if (existing.itemID == itemSO.itemID)
                {
                    existing.quantity += quantity;
                    Debug.Log($"Stacked! New quantity = {existing.quantity}");
                    OnInventoryChanged?.Invoke();
                    SaveManager.Instance?.SaveGame();
                    return;
                }
            }
        }

        // Add new item
        items.Add(new ItemInstance(itemSO, quantity));
        Debug.Log($"Added new item. Total items now: {items.Count}");
        
        OnInventoryChanged?.Invoke();
        SaveManager.Instance?.SaveGame();
    }

    public void TriggerInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void ClearInventory()
    {
        items.Clear();
        Debug.Log("Inventory cleared.");
        OnInventoryChanged?.Invoke();
        SaveManager.Instance?.SaveGame();
    }
}

