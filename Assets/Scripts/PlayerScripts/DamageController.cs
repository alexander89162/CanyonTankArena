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
            
            //health.SetFullHealth();  // Reset health to max for testing purposes
            Debug.Log($"[DamageController] {gameObject.name} has been destroyed!");
            
        }
    }
    [ContextMenu("Test damage")]
    public void TestDamage() => TakeDamage(5);
}