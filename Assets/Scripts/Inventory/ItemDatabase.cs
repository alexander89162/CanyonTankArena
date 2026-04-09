using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [SerializeField] private List<ItemSO> allRegisteredItems = new List<ItemSO>();

    private Dictionary<string, ItemSO> itemLookup = new();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDatabase();
    }

    private void BuildDatabase()
    {
        itemLookup.Clear();
        foreach (var item in allRegisteredItems)
            if (!string.IsNullOrEmpty(item.itemID))
                itemLookup[item.itemID] = item;
    }

    public ItemSO GetItemByID(string id) => itemLookup.TryGetValue(id, out var item) ? item : null;
}