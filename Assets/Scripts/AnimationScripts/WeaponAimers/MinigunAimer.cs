using UnityEngine;

/*Aimer implementation for minigun.
NOTE: this aimer has rig issues so offsets are hardcoded into the angle math*/
public class MinigunAimer : WeaponAimer
{
    [SerializeField] private Transform minigunBase;
    [SerializeField] private Transform minigunBody;
    [SerializeField] private Transform minigunBarrels;
    [SerializeField] private Transform barrelEnd;
    [SerializeField] private float lowerTarget = 4f; // make the gun point lower to better face the target
    public Vector3 bulletSpawnOffset = new Vector3(0.5f, 0f, 3f);
    public float bulletDamage = 12f;
    public float projectileSpeed = 350f;
    public float bulletMaxLifetime = 2f;

    private Quaternion baseRestRotation;
    private Quaternion bodyRestRotation;
    private Quaternion barrelsRestRotation;

    protected override void Awake()
    {
        base.Awake();
        baseRestRotation    = minigunBase.localRotation;
        bodyRestRotation    = minigunBody.localRotation;
        barrelsRestRotation = minigunBarrels.localRotation;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(toEnemy.y, new Vector2(toEnemy.x, toEnemy.z).magnitude) * Mathf.Rad2Deg;
        pitch += lowerTarget;

        minigunBase.localRotation = baseRestRotation * Quaternion.Euler(0, yaw, 0);
        minigunBody.localRotation = bodyRestRotation * Quaternion.Euler(pitch, 0, 0);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (fireTimer > 0) return;
        fireTimer = fireCooldown;

        // TODO: check current ammo and state

        Vector3 targetPos = targetPosition;
        Vector3 origin = barrelEnd.position;

        Vector3 dir = (targetPos - origin).normalized;

        Bullet b = new Bullet
        {
            position = barrelEnd.position + barrelEnd.TransformDirection(bulletSpawnOffset),
            velocity = dir * projectileSpeed,
            damage = bulletDamage,
            type = 1,
            remainingLifetime = bulletMaxLifetime,
            owner = gameObject
        };

        ProjectileManager.Instance.SpawnBullet(b);
        minigunBarrels.localRotation *= Quaternion.Euler(0, 20, 0); // temporary
    }

    public override void ReloadWeapon(){} // do nothing for now

    public override void OnWeaponSwapped()
    {
        fireTimer = 1.1f;
    }
}