using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MiniTank/Tech Tree Data", fileName = "TechTreeData")]
public class TechTreeData : ScriptableObject
{
    public List<TechTreeNode> nodes = new List<TechTreeNode>();

    [System.Serializable]
    public class Edge
    {
        public string fromId;
        public string toId;
    }

    public List<Edge> edges = new List<Edge>();

    public TechTreeNode GetNodeById(string id)
    {
        return nodes.Find(n => n != null && n.id == id);
    }
}
