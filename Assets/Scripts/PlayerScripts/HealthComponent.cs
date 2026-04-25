using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
        if (gameObject.CompareTag("Player") && PlayerTankStats.Instance != null)
        {
            PlayerTankStats.Instance.ApplyTechBonuses();
            maxHealth = PlayerTankStats.Instance.maxHealth;
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }
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
                GetComponent<EnemyDrop>()?.OnDeath();
                Destroy(gameObject, deathDelay);

                if (gameObject.CompareTag("Player"))
                {
                    ScoreManager.Instance?.SaveHighScore();
                    SceneManager.LoadScene("StartMenu"); // Reload current scene on player death
                }
            }
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(HealthNormalized);
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        // Preserve current health ratio when max health increases
        float ratio = MaxHealth > 0 ? currentHealth / MaxHealth : 1f;
        
        maxHealth = newMaxHealth;
        currentHealth = maxHealth * ratio;
        
        OnHealthChanged?.Invoke(HealthNormalized);
    }

    public void SetFullHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(1f);
    }
}