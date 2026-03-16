using UnityEngine;

public class DamageController : MonoBehaviour
{
    [SerializeField] private HealthComponent health;

    private void Awake()
    {
        // Try to auto-find if not assigned
        health ??= GetComponent<HealthComponent>();
    }

    public void TakeDamage(float damageAmount)
    {

        float remainingDamage = damageAmount;
    
        health.TakeDamage(remainingDamage);

        if (health.IsDead)
        {
            
            //health.SetFullHealth();
            Debug.Log($"[DamageController] {gameObject.name} has been destroyed!");
            // Optional: Add death effects, disable tank controls, etc.
            
        }
    }
    [ContextMenu("Test damage")]
    public void TestDamage() => TakeDamage(5);
}