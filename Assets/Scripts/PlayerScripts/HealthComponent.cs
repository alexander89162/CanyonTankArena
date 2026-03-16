using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;       // for enemies
    [SerializeField] private float deathDelay = 0.1f;          // small delay for death animation

    public UnityEvent<float> OnHealthChanged;       // normalized 0–1
    public UnityEvent OnDeath;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => Mathf.Clamp01(currentHealth / maxHealth);
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = newMaxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(HealthNormalized);
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0) return;
        float oldHP = currentHealth;

        currentHealth = Mathf.Max(0f, oldHP - amount);
        OnHealthChanged?.Invoke(HealthNormalized);

        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            OnDeath?.Invoke();

            if (destroyOnDeath)
            {
                Destroy(gameObject, deathDelay);
            }
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(HealthNormalized);
    }

    public void SetFullHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(1f);
    }
}