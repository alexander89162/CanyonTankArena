using UnityEngine;
using System.IO;

public enum ItemRarity { 
    Common, 
    Uncommon, 
    Rare, 
    Epic, 
    Legendary 
    }

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public abstract class ItemSO : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    [TextArea(3, 6)] public string description = "";
    public Sprite icon;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Stacking")]
    public bool canStack = true;
    public int maxStackSize = 999;
    public AudioClip pickupSound;


    [Header("Multiplayer & Save")]
    public string itemID = "";

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
