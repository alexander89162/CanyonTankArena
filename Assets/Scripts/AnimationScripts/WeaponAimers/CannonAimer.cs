using UnityEngine;

/*Aimer implementation for the basic cannon
Supports aiming at the target and clamping cannon-body at clampLiftCannon,
adding any remaining up/down aim via cannon-barrel, up until clampLiftBarrel*/
public class CannonAimer : WeaponAimer
{
    [SerializeField] private Transform cannonBody;
    [SerializeField] private Transform cannonBarrel;
    [SerializeField] private Transform barrelEnd;
    [SerializeField] private float clampLiftCannon;
    [SerializeField] private float clampLiftBarrel;
    public Vector3 shellSpawnOffset = new Vector3(0f, 0f, 3f);
    public float shellDamage = 35f;
    public float projectileSpeed = 150f;
    public float shellMaxLifetime = 4f;
    
    private Quaternion bodyRestRotation;
    private Quaternion barrelRestRotation;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation   = cannonBody.localRotation;
        barrelRestRotation = cannonBarrel.localRotation;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(toEnemy.y, new Vector2(toEnemy.x, toEnemy.z).magnitude) * Mathf.Rad2Deg;

        float cannonBodyPitch = Mathf.Clamp(pitch, -clampLiftCannon, clampLiftCannon);
        float barrelPitch = Mathf.Clamp(pitch - cannonBodyPitch, -clampLiftBarrel, clampLiftBarrel);

        cannonBody.localRotation = bodyRestRotation * Quaternion.Euler(-cannonBodyPitch, yaw, 0);
        cannonBarrel.localRotation = barrelRestRotation * Quaternion.Euler(barrelPitch, 0, 0);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (fireTimer > 0) return;
        fireTimer = fireCooldown;

        // TODO: check current ammo

        Vector3 targetPos = targetPosition;
        Vector3 origin = barrelEnd.position;

        Vector3 dir = (targetPos - origin).normalized;

        Bullet b = new Bullet
        {
            position = barrelEnd.position + barrelEnd.TransformDirection(shellSpawnOffset),
            velocity = dir * projectileSpeed,
            damage = shellDamage,
            type = 0,
            remainingLifetime = shellMaxLifetime,
            owner = gameObject
        };

        ProjectileManager.Instance.SpawnBullet(b);
    }

    public override void ReloadWeapon(){} // TODO

    public override void OnWeaponSwapped()
    {
        fireTimer = 0.5f;
    }

    public override void DoWhileHolding()
    {
        //
    }
}