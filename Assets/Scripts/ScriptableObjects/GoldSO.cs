using UnityEngine;

[CreateAssetMenu(fileName = "GoldSO", menuName = "Scriptable Objects/GoldSO")]
public class GoldSO : ItemSO
{
    private void OnEnable()
    {
        itemName = "Gold"; 
        description = "Used for cosmetic colors";
        canStack = true; 
        maxStackSize = 999999; 
        rarity = ItemRarity.Common;
    }
}