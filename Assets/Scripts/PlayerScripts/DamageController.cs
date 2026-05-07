using UnityEngine;

public class DamageController : MonoBehaviour
{
    [SerializeField] private HealthComponent health;

    [Header("Sound")]
    [SerializeField] private AudioSource hitAudioSource;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;

    private void Awake()
    {
        health ??= GetComponent<HealthComponent>();
    }

    // Original signature kept so nothing else breaks
    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, null, null);
    }

    public void TakeDamage(float damageAmount, Vector3? hitPosition, Vector3? hitNormal = null)
    {
        health.TakeDamage(damageAmount);

        if (hitAudioSource != null)
            hitAudioSource.Play();

        if (hitEffectPrefab != null)
        {
            Vector3 spawnPos = hitPosition ?? transform.position;

            // Use surface normal for rotation so sparks fly out correctly,
            // fall back to direction away from object center
            Vector3 outDir = hitNormal ?? (spawnPos - transform.position);
            Quaternion rot = outDir != Vector3.zero
                ? Quaternion.LookRotation(outDir.normalized)
                : Quaternion.identity;

            Instantiate(hitEffectPrefab, spawnPos, rot);
        }

        if (health.IsDead)
            Debug.Log($"[DamageController] {gameObject.name} has been destroyed!");
    }

    [ContextMenu("Test damage")]
    public void TestDamage() => TakeDamage(5);
}