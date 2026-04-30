using UnityEngine;
using System.Collections.Generic;

public class TechTreeController : MonoBehaviour
{
    [SerializeField] private TechTreeSceneView sceneView;

    private void Start()
    {
        RefreshTree();
    }

    public void RefreshTree()
    {
        if (sceneView == null) return;

        TechTreeData data = new TechTreeData();

        var allNodes = TechTreeManager.Instance.GetAllTechNodes();

        // Create nodes with positions
        foreach (var techNode in allNodes)
        {
            TechTreeNode node = new TechTreeNode
            {
                id = techNode.nodeID,
                nodeName = techNode.nodeName,
                position = techNode.editorPosition,
                description = techNode.description,
                cost = techNode.requiredItems.Count,
                costText = BuildCostString(techNode),
                locked = !TechTreeManager.Instance.IsNodeUnlocked(techNode),
                icon = techNode.icon
            };
            data.nodes.Add(node);
        }

        // Create edges (connections) based on prerequisites
        foreach (var techNode in allNodes)
        {
            foreach (var prereq in techNode.prerequisites)
            {
                if (prereq != null)
                {
                    data.edges.Add(new TechTreeData.Edge
                    {
                        fromId = prereq.nodeID,
                        toId = techNode.nodeID
                    });
                }
            }
        }

        sceneView.SetData(data);
    }

    private string BuildCostString(TechNodeSO node)
{
    if (node == null) return "";

    List<string> costs = new List<string>();

    foreach (var itemCost in node.requiredItems)
    {
        if (itemCost.item != null && itemCost.amount > 0)
        {
            costs.Add($"{itemCost.amount} {itemCost.item.itemName}");
        }
    }

    if (costs.Count == 0)
        return "Free";

    return string.Join(" + ", costs);
}

    public void OnSkillUnlocked()
    {
        RefreshTree();
    }
}