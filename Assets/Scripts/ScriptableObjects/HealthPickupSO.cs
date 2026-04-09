using UnityEngine;

[CreateAssetMenu(fileName = "HealthPickupSO", menuName = "Scriptable Objects/HealthPickupSO")]
public class HealthPickupSO : ItemSO
{
    public float healthRestoreAmount = 30f;
    private void OnEnable() { 
        canStack = true; 
        maxStackSize = 20; 
        rarity = ItemRarity.Uncommon; 
        }
}
