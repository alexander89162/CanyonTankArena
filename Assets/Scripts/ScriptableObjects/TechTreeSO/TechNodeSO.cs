using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewTechNode", menuName = "Tech Tree/Tech Node")]
public class TechNodeSO : ScriptableObject
{
    [Header("Basic Info")]
    public string nodeName = "New Tech Node";
    [TextArea(3, 6)] public string description = "";
    public Sprite icon;

   
    [Header("Item Costs")]
    public List<ItemCost> requiredItems = new List<ItemCost>();

    [Header("Effects")]
    public float damageBonus = 0f;
    public float cannonDamageBonus = 0f;
    public float minigunDamageBonus = 0f;
    public float healthBonus = 0f;
    public float speedBonus = 0f;
    public float fireRateBonus = 0f;

    public List<TechNodeSO> prerequisites = new List<TechNodeSO>();

    [Header("Editor")]
    public Vector2 editorPosition = Vector2.zero;

    [Header("Save")]
    public string nodeID = "";

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(nodeID))
            nodeID = System.Guid.NewGuid().ToString();
    }
}

[System.Serializable]
public class ItemCost
{
    public ItemSO item;
    public int amount = 1;
}