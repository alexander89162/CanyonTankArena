using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cannonShellRadius = 1.5f;
    [SerializeField] private float bulletRadius = 0.1f;

    private Bullet[] bullets;
    private HitEvent[] hitBuffer;
    private int activeCount = 0;
    private int hitCount = 0;
    private float[] radiusLookup;

    public struct HitEvent
    {
        public Vector3 point;
        public Vector3 normal;
        public float damage;
        public GameObject target;
        public byte bulletType;
    }

    void Awake()
    {
        bullets = new Bullet[1024];
        hitBuffer = new HitEvent[1024];
        radiusLookup = new float[256];

        radiusLookup[0] = cannonShellRadius;
        radiusLookup[1] = bulletRadius;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1) process bullet movement
        for (int i = 0; i < activeCount;)
        {
            if (!Process(ref bullets[i], dt))
            {
                DestroyBullet(i);
            }
            else
            {
                i++;
            }
        }

        // 2) process bullet hits
        for (int i = 0; i < hitCount; i++)
        {
            ApplyHit(hitBuffer[i]);
        }

        hitCount = 0;
    }

    public void SpawnBullet(Bullet bullet)
    {
        if (activeCount >= bullets.Length) return;

        bullets[activeCount] = bullet;
        activeCount++;
    }

    private void DestroyBullet(int index)
    {
        activeCount--;
        bullets[index] = bullets[activeCount];
    }

    private bool Process(ref Bullet bullet, float dt)
    {
        bullet.remainingLifetime -= dt;
        if (bullet.remainingLifetime <= 0f) return false;

        Vector3 oldPosition = bullet.position;
        Vector3 movement = bullet.velocity * dt;
        Vector3 newPosition = bullet.position + movement;
        
        float distance = movement.magnitude;
        if (distance > 0f)
        {
            Vector3 direction = movement / distance;

            if (Physics.SphereCast(
                oldPosition,
                radiusLookup[bullet.type],
                direction,
                out RaycastHit hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
            {
                if (hitCount < hitBuffer.Length)
                {
                    hitBuffer[hitCount++] = new HitEvent
                    {
                        point = hit.point,
                        normal = hit.normal,
                        damage = bullet.damage,
                        target = hit.collider.transform.root.gameObject,
                        bulletType = bullet.type
                    };
                }

                bullet.position = hit.point + hit.normal * 0.01f;
                return false; // destroy bullet
            }
        }

        bullet.position = newPosition;
        return true;
    }

    private void ApplyHit(HitEvent hit)
    {
        if (hit.target == null) return;

        var damageController = hit.target.GetComponent<DamageController>();
        if (damageController != null)
        {
            damageController.TakeDamage(hit.damage);
        }

        // TODO: VFX spawn (?)
    }
}