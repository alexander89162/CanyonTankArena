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
        bullet.position += bullet.velocity * dt;
        Vector3 movement = bullet.position - oldPosition;
        float distance = movement.magnitude;
        if (distance > 0f)
        {
            Vector3 direction = movement / distance;

            if (Physics.SphereCast(
                oldPosition,
                GetRadius(bullet.type),
                direction,
                out RaycastHit hit,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
            {
                bullet.position = hit.point + hit.normal * 0.01f;
                return false; // destroy bullet
            }
        }

        return true;
    }

    private float GetRadius(byte type)
    {
        switch (type)
        {
            case 0: return cannonShellRadius; // cannon shell
            case 1: return bulletRadius;  // regular bullet
            default: return 1f;
        }
    }

    private void ApplyHit(HitEvent hit)
    {
        // damage system
        // armor logic
        // VFX spawn
        // sound
    }
}