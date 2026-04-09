using UnityEngine;

[CreateAssetMenu(fileName = "TechTreeSkillPointSO", menuName = "Scriptable Objects/TechTreeSkillPointSO")]
public class TechTreeSkillPointSO : ItemSO
{
    private void OnEnable() { 
        canStack = true; 
        maxStackSize = 999; 
        rarity = ItemRarity.Epic; 
        }
}
