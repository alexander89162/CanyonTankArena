using UnityEngine;

public class PickupDrops : MonoBehaviour
{
    [HideInInspector] public ItemInstance itemInstance;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (PlayerInventory.Instance != null && itemInstance != null)
            {
                ItemSO so = itemInstance.GetItemSO();
                PlayerInventory.Instance.AddItem(so, itemInstance.quantity);
                Debug.Log($"✅ Picked up: {itemInstance.quantity}x {so.itemName}");
            }
            Destroy(gameObject);
        }
    }
}