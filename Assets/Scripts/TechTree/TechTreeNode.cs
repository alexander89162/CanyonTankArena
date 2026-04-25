using System;
using UnityEngine;

[CreateAssetMenu(menuName = "MiniTank/Tech Tree Node", fileName = "TechTreeNode")]
public class TechTreeNode : ScriptableObject
{
    // stable id used for edges
    public string id;

    public string nodeName = "New Node";
    [TextArea(3, 6)] public string description;
    public int cost = 0;
    public Sprite icon;

    // position on the editor canvas (in pixels)
    public Vector2 position = Vector2.zero;

    // optional locked state for UI
    public bool locked = false;

    public int column = 0;
    public int row = 0;
}
