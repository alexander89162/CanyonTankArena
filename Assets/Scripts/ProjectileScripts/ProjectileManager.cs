using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cannonShellRadius = 1.5f;
    [SerializeField] private float bulletRadius = 0.1f;

    private Bullet[] bullets;
    private int activeCount = 0;

    void Awake()
    {
        bullets = new Bullet[1024];
    }

    void Update()
    {
        float dt = Time.deltaTime;

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
            default: return 0.1f;
        }
    }
}