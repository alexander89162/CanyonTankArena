using UnityEngine;

public class PlayerTankStats : MonoBehaviour
{
    public static PlayerTankStats Instance { get; private set; }

    [Header("Base Stats")]
    public float baseMaxHealth = 100f;
    public float baseDamageMultiplier = 0f;
    public float baseCannonDamageMultiplier = 0f;
    public float baseMinigunDamageMultiplier = 0f;
    public float baseSpeedMultiplier = 0f;
    public float baseFireRateMultiplier = 0f;

    [Header("Current Stats (Modified by Skills)")]
    public float maxHealth;
    public float damageMultiplier = 0f;
    public float cannonDamageMultiplier = 0f;
    public float minigunDamageMultiplier = 0f;
    public float speedMultiplier = 0f;
    public float fireRateMultiplier = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyTechBonuses();
    }

    private void Start()
    {
        ResetToBaseStats();
        ApplyTechBonuses();
        ApplyToCurrentPlayerTank();
    }


    public void ResetToBaseStats()
    {
        maxHealth = baseMaxHealth;
        damageMultiplier = 0f;
        cannonDamageMultiplier = 0f;
        minigunDamageMultiplier = 0f;
        speedMultiplier = 0f;
        fireRateMultiplier = 0f;
    }

    public void ApplyTechBonuses()
    {
        ResetToBaseStats();

        if (TechTreeManager.Instance != null) 
        {

            foreach (var node in TechTreeManager.Instance.GetAllUnlockedNodes())
            {
                maxHealth += node.healthBonus;
                damageMultiplier += node.damageBonus;
                cannonDamageMultiplier += node.cannonDamageBonus;
                minigunDamageMultiplier += node.minigunDamageBonus;
                speedMultiplier += node.speedBonus;
                fireRateMultiplier += node.fireRateBonus;
            }

            Debug.Log("Tech bonuses applied to tank stats");

        }
    }

    public void ApplyToCurrentPlayerTank()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("TankStats: No Player tagged object found in scene.");
            return;
        }

        HealthComponent health = player.GetComponentInChildren<HealthComponent>();
        if (health != null)
        {
            health.SetMaxHealth(maxHealth);
            Debug.Log("TankStats: Applied bonuses to player tank");
        }
    }
}