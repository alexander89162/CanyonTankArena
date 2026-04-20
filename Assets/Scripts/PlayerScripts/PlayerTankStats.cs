using UnityEngine;

public class PlayerTankStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseMaxHealth = 100f;
    public float baseDamageMultiplier = 1f;
    public float baseSpeedMultiplier = 1f;
    public float baseFireRateMultiplier = 1f;

    [Header("Current Stats (Modified by Skills)")]
    public float maxHealth;
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float fireRateMultiplier = 1f;

    private void Start()
    {
        ResetToBaseStats();
        ApplySkillBonuses();
    }


    public void ResetToBaseStats()
    {
        maxHealth = baseMaxHealth;
        damageMultiplier = 1f;
        speedMultiplier = 1f;
        fireRateMultiplier = 1f;
    }

    public void ApplySkillBonuses()
    {
        ResetToBaseStats();

        if (TechTreeManager.Instance == null) return;

        foreach (var node in TechTreeManager.Instance.GetAllUnlockedNodes())
        {
            maxHealth += node.healthBonus;
            damageMultiplier += node.damageBonus;
            speedMultiplier += node.speedBonus;
            fireRateMultiplier += node.fireRateBonus;
        }

        Debug.Log("Tech bonuses applied to tank stats");
    }
}