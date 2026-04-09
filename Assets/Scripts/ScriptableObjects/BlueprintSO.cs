using UnityEngine;

[CreateAssetMenu(fileName = "BlueprintSO", menuName = "Scriptable Objects/BlueprintSO")]
public class BlueprintSO : ItemSO
{
    private void OnEnable() { 
        canStack = true; 
        maxStackSize = 999; 
        rarity = ItemRarity.Legendary; 
        }
}
