using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public ItemSO item;
    [Range(0f, 100f)] public float weight = 10f;
    public int minAmount = 1;
    public int maxAmount = 1;
}

[CreateAssetMenu(fileName = "DropTableSO", menuName = "Scriptable Objects/DropTableSO")]
public class DropTableSO : ScriptableObject
{
    public List<DropEntry> entries = new List<DropEntry>();

    public List<ItemInstance> RollDrops()
    {
        var drops = new List<ItemInstance>();

        if (entries.Count == 0) return drops;

        float totalWeight = 0f;
        foreach (var e in entries) if (e.item != null) totalWeight += e.weight;

        if (totalWeight <= 0) return drops;

        float roll = Random.Range(0f, totalWeight);
        foreach (var entry in entries)
        {
            if (entry.item == null) continue;
            if (roll < entry.weight)
            {
                int qty = Random.Range(entry.minAmount, entry.maxAmount + 1);
                drops.Add(new ItemInstance(entry.item, qty));
                return drops; // one random drop for simplicity
            }
            roll -= entry.weight;
        }
        return drops;
    }
}