using UnityEditor.Experimental.GraphView;
using UnityEngine;

/*Drives the missile's physics simulation and handles explosion upon 
collision by its gameObject's RigidBody component.*/
public class Missile : MonoBehaviour
{
    public float lifeTime = 10f;
    public GameObject explosionPrefab;
    public LayerMask triggerExplosionMask;
    public LayerMask damageMask;
    public float explosionDamage = 20f;
    public float explosionInnerRadius = 8f;
    public float explosionOuterRadius = 20f;
    public AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Rigidbody rb;
    private Vector3 currentVelocity;
    private float gravityMultiplier = 9.81f;
    private float forwardAcceleration;
    private float lifetimeRemaining = -1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        int ownerLayer = gameObject.layer;
        damageMask &= ~(1 << ownerLayer); // no damage to self
    }

    void Update()
    {
        if (lifetimeRemaining == -1) return;
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f) { Destroy(gameObject); return; }

        // 1) Handle movement
        currentVelocity += currentVelocity.normalized * forwardAcceleration * Time.deltaTime;
        currentVelocity += Vector3.down * gravityMultiplier * Time.deltaTime;
        rb.MovePosition(rb.position + currentVelocity * Time.deltaTime);

        // 2) adjust orientation to match movement
        transform.rotation = Quaternion.LookRotation(currentVelocity);
    }

    public void Launch(Vector3 targetPosition, 
        float missileLaunchSpeed, float missileForwardAcceleration, 
        float missileGravityMultiplier)
    {
        gravityMultiplier = missileGravityMultiplier;
        forwardAcceleration = missileForwardAcceleration;

        currentVelocity = transform.forward * missileLaunchSpeed;
        lifetimeRemaining = lifeTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        int hitLayer = collision.gameObject.layer;

        bool shouldExplode = (triggerExplosionMask.value & (1 << hitLayer)) != 0;

        if (shouldExplode)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 1) Spawn explosion prefab
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // 2) Apply damage to nearby enemies
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionOuterRadius, damageMask);

        foreach (var hit in hits)
        {
            DamageController damage = hit.GetComponentInParent<DamageController>();
            if (damage != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float t = Mathf.InverseLerp(explosionInnerRadius, explosionOuterRadius, distance);
                float falloff = damageFalloff.Evaluate(t);
                float dealtDamage = explosionDamage * falloff;
                damage.TakeDamage(dealtDamage);
            }
        }

        Destroy(gameObject);
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawSphere(transform.position, explosionOuterRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, explosionInnerRadius);

        // optional: visualize curve samples
        Gizmos.color = Color.white;

        int steps = 16;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float radius = Mathf.Lerp(explosionInnerRadius, explosionOuterRadius, t);
            float falloff = damageFalloff.Evaluate(t);

            Gizmos.color = Color.Lerp(Color.blue, Color.red, falloff);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
    #endif
}