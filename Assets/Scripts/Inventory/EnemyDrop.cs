using UnityEngine;

public class EnemyDrop : MonoBehaviour
{
    [SerializeField] private DropTableSO dropTable;
    
    [Header("Loot Settings")]
    [SerializeField] private float dropChance = 0.85f;

    private DropSpawner spawner;

    private void Awake()
    {
        // Auto-find the spawner in the scene
        spawner = Object.FindFirstObjectByType<DropSpawner>();
        
        if (spawner == null)
        {
            Debug.LogWarning("DropSpawner not found in scene! Loot will not spawn physically.");
        }
    }

    public void OnDeath()
    {
        if (dropTable == null) return;
        if (Random.value > dropChance) return;

        var drops = dropTable.RollDrops();

        foreach (var drop in drops)
        {
            if (spawner != null)
            {
                // Spawn physical pickup
                spawner.SpawnPickup(drop, transform.position);
            }
            else
            {
                // Fallback: Add directly to inventory if no spawner
                if (PlayerInventory.Instance != null)
                {
                    PlayerInventory.Instance.AddItem(drop.GetItemSO(), drop.quantity);
                }
            }
        }
    }
}