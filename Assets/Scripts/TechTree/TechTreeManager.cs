using UnityEngine;
using System.Collections.Generic;

public class TechTreeManager : MonoBehaviour
{
    public static TechTreeManager Instance { get; private set; }

    [SerializeField] private List<TechNodeSO> allTechNodes = new List<TechNodeSO>();

    private PlayerTechData currentTechData = new PlayerTechData();

    public List<TechNodeSO> GetAllTechNodes() => allTechNodes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

   public bool CanUnlockNode(TechNodeSO node)
    {
        if (node == null || currentTechData.unlockedNodeIDs.Contains(node.nodeID))
            return false;

        // Check prerequisites
        foreach (var prereq in node.prerequisites)
        {
            if (prereq != null && !IsNodeUnlocked(prereq))
                return false;
        }

        // Check item costs
        foreach (var cost in node.requiredItems)
        {
            if (cost.item == null) continue;
            if (PlayerInventory.Instance.GetItemCount(cost.item.itemID) < cost.amount)
                return false;
        }

        return true;
    }

    public bool UnlockNode(TechNodeSO node)
    {
        if (!CanUnlockNode(node)) return false;

        // Spend items
        foreach (var cost in node.requiredItems)
        {
            if (cost.item != null)
            {
                PlayerInventory.Instance?.RemoveItem(cost.item.itemID, cost.amount);
            }
        }

        // Unlock
        currentTechData.unlockedNodeIDs.Add(node.nodeID);
        
        if (PlayerTankStats.Instance != null)
        {
            PlayerTankStats.Instance.ApplyTechBonuses();
            PlayerTankStats.Instance.ApplyToCurrentPlayerTank();
        }

        SaveTechTree();
        Debug.Log($" Unlocked tech: {node.nodeName}");
        //TechTreeSceneView.Instance?.QueueRebuild();
        return true;
    }

    public bool IsNodeUnlocked(TechNodeSO node)
    {
        return currentTechData.unlockedNodeIDs.Contains(node.nodeID);
    }

    // Add this to SkillTreeManager
    public List<TechNodeSO> GetAllUnlockedNodes()
    {
        List<TechNodeSO> unlocked = new List<TechNodeSO>();
        foreach (var node in allTechNodes)
        {
            if (IsNodeUnlocked(node))
                unlocked.Add(node);
        }
        return unlocked;
    }

    public PlayerTechData GetCurrentTechData()
    {

        return currentTechData;
    }

    public List<string> GetUnlockedNodeIDs()
    {
        return new List<string>(currentTechData.unlockedNodeIDs);
    }

    public void LoadFromData(PlayerTechData loadedData)
    {
        currentTechData = loadedData;
        if (PlayerTankStats.Instance != null)
            PlayerTankStats.Instance.ApplyTechBonuses();
    }

    // Save / Load
    public void SaveTechTree()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveTechData(currentTechData);
    }

    public void LoadTechTree()
    {
        if (SaveManager.Instance != null)
            currentTechData = SaveManager.Instance.LoadTechData();
    }
}