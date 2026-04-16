using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpikeballDamage : MonoBehaviour
{
    [SerializeField] private float grazeDamage = 5f;
    [SerializeField] private float launchDamage = 30f;
    [SerializeField] private float minDamageSpeed = 5f;
    [SerializeField] private float heavyImpactSpeed = 20f;

    [SerializeField] private string playerTag = "Player";

    private Rigidbody rb;
    private SpikeballController controller; // Optional: to read state

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<SpikeballController>();
    }

    //handles the collision of the spikeball and applies dmg function
    void OnCollisionEnter(Collision collision)
    {
        // Only check for player hits
        if (!collision.gameObject.CompareTag(playerTag))
            return;

        HealthComponent playerHealth = collision.gameObject.GetComponentInParent<HealthComponent>();
        if (playerHealth == null) return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minDamageSpeed) return;

        float damage = CalculateDamage(impactSpeed);

        playerHealth.TakeDamage(damage);

        #if UNITY_EDITOR
        if (controller != null && controller.debug)
            Debug.Log($"Spikeball hit player → {damage:F0} dmg (speed: {impactSpeed:F1})");
        #endif
    }
    
    //helper func to check which damage to apply
    private float CalculateDamage(float impactSpeed)
    {
        // Heavy damage during/after launch (before bouncing or very fast)
        if (IsHeavyLaunchHit() || impactSpeed >= heavyImpactSpeed)
        {
            return launchDamage;
        }

        return grazeDamage;
    }

    //dmg func for spikeball launch dmg
    private bool IsHeavyLaunchHit()
    {
        if (controller == null) return false;

        // Heavy damage if it hasn't bounced yet (fresh launch)
        return controller.currentState == SpikeballController.AttackState.FreeRoam 
               && !controller.hasBouncedThisLaunch;
    }
}