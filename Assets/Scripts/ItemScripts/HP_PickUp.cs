using UnityEngine;

public class HP_PickUp : MonoBehaviour
{
    [SerializeField] private float healAmount = 40f;           // How much HP to restore
    [SerializeField] private bool destroyOnPickup = true;      // Remove medkit after use

    private void OnTriggerEnter(Collider other)
    {
        // Only player should be able to pick it up
        if (!other.CompareTag("Player")) return;

        // Try to find the health component
        HealthComponent health = other.GetComponentInParent<HealthComponent>();

        if (health != null && !health.IsDead)
        {
            //Heal the player
            health.Heal(healAmount);

            //Remove the medkit
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }
}