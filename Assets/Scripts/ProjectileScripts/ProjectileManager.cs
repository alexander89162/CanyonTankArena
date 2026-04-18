using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public Bullet[] bullets;

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

            if (Physics.Raycast(oldPosition, direction, out RaycastHit hit, distance))
            {
                bullet.position = hit.point;
                return false; // destroy bullet
            }
        }

        return true;
    }
}