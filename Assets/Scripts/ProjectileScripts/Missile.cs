using UnityEditor.Experimental.GraphView;
using UnityEngine;

/*Drives the missile's physics simulation and handles explosion upon 
collision by its gameObject's RigidBody component.*/
public class Missile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float explosionDamage = 20f;
    public float explosionInnerRadius = 8f;
    public float explosionOuterRadius = 20f;
    public AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float explosionScale = 1f;

    private Rigidbody rb;
    private LayerMask triggerExplosionMask;
    private LayerMask damageMask;
    private Transform ownerRoot;
    private Vector3 currentVelocity;
    private float gravityMultiplier = 9.81f;
    private float forwardAcceleration;
    private float lifetimeRemaining = -1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        float missileGravityMultiplier, float missileExplosionScale,
        Vector3 hostVelocity, LayerMask triggerExplosionMask, LayerMask damageMask, 
        Transform launcherRoot)
    {
        this.triggerExplosionMask = triggerExplosionMask;
        this.damageMask = damageMask;
        ownerRoot = launcherRoot;
        gravityMultiplier = missileGravityMultiplier;
        forwardAcceleration = missileForwardAcceleration;

        currentVelocity = transform.forward * missileLaunchSpeed + hostVelocity;
        lifetimeRemaining = lifeTime;
        explosionScale = missileExplosionScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        int hitLayer = collision.gameObject.layer;

        if (ShouldExplode(hitLayer, collision))
            Explode();
    }

    private bool ShouldExplode(int hitLayer, Collision collision)
    {
        // if triggered by self (same root), don't explode
        if (collision.transform.root == ownerRoot)
            return false;

        // hit layer is in triggerExplosionMask --> return true
        return (triggerExplosionMask.value & (1 << hitLayer)) != 0;
    }

    private void Explode()
    {
        // 1) Spawn explosion prefab
        ExplosionPool.Instance.Spawn(transform.position, Quaternion.identity, explosionScale);

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