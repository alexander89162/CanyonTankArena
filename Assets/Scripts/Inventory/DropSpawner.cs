using UnityEngine;
using System.Collections.Generic;

public class DropSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnHeightOffset = 0.6f;

    [Header("Item Prefabs")]
    public GameObject healthPickupPrefab;
    public GameObject goldPickupPrefab;

    public void SpawnPickup(ItemInstance itemInstance, Vector3 spawnPosition)
    {
        if (itemInstance == null) return;

        ItemSO itemSO = itemInstance.GetItemSO();
        if (itemSO == null) return;

        GameObject prefab = GetPrefabForItem(itemSO);
        if (prefab == null)
        {
            Debug.LogWarning($"No prefab assigned for: {itemSO.itemName}");
            return;
        }

        Vector3 finalPos = spawnPosition + Vector3.up * spawnHeightOffset;
        GameObject pickupObj = Instantiate(prefab, finalPos, Quaternion.identity);

        // Pop effect
        var popEffect = pickupObj.GetComponent<DropEffect>() ?? pickupObj.AddComponent<DropEffect>();
        popEffect.PopOut();

        // Pickup logic
        var behavior = pickupObj.GetComponent<PickupDrops>() ?? pickupObj.AddComponent<PickupDrops>();
        behavior.itemInstance = itemInstance;

        Debug.Log($"Spawned: {itemInstance.quantity}x {itemSO.itemName}");
    }

    private GameObject GetPrefabForItem(ItemSO itemSO)
    {
        switch (itemSO)
        {
            case HealthPickupSO _:      return healthPickupPrefab;
            case GoldSO _:              return goldPickupPrefab;

            default:
                Debug.LogWarning($"Unknown item type: {itemSO.GetType().Name}");
                return null;
        }
    }
}