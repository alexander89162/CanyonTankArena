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
    public float explosionRadius = 20f;

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
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageMask);

        foreach (var hit in hits)
        {
            DamageController damage = hit.GetComponentInParent<DamageController>();
            if (damage != null) 
                damage.TakeDamage(explosionDamage);
        }

        Destroy(gameObject);
    }
}