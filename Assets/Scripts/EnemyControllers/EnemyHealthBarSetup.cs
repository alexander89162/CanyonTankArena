using UnityEngine;

public class EnemyHealthBarSetup : MonoBehaviour
{
    [SerializeField] private GameObject healthBarPrefab;   // Drag your EnemyHealthBarPrefab here
    [SerializeField] private float spawnHeight = 2.8f;

    private void Awake()
    {
        if (healthBarPrefab == null) return;

        GameObject barObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
        barObj.transform.SetParent(transform);  // Attach to enemy

        // Position slightly above
        barObj.transform.localPosition = new Vector3(0, spawnHeight, 0);

        EnemyHealthBar healthBar = barObj.GetComponent<EnemyHealthBar>();
        HealthComponent health = GetComponent<HealthComponent>();

        if (healthBar != null && health != null)
        {
            healthBar.Initialize(health);
        }
    }
}