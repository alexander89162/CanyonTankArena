using UnityEngine;
using PrimeTween;

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
    public float barrelsSpinSpeed = 800f;
    public float delayBeforeFire = 2.2f;
    public float cooldownMultiplier = 2f;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Quaternion baseRestRotation;
    private Quaternion bodyRestRotation;
    private float spinningTime = 0f;
    private bool firedLastFrame = false; // synced internally, not necessarily to real current frame
    private bool weaponLocked = false; // lock during reload, stun, etc

    protected override void Awake()
    {
        base.Awake();
        baseRestRotation = minigunBase.localRotation;
        bodyRestRotation = minigunBody.localRotation;

        if (gameObject.CompareTag("Player") && PlayerTankStats.Instance != null && PlayerTankStats.Instance.minigunDamageMultiplier != 0f)
        {
            //PlayerTankStats.Instance.ApplyTechBonuses();
            bulletDamage += bulletDamage * PlayerTankStats.Instance.minigunDamageMultiplier;
        }
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
        if (currentAmmo > 0) firedLastFrame = true; // add rotation even when not ready to shoot bullets
        else
        {
            ReloadWeapon();
            return;
        }

        if (fireTimer > 0 || spinningTime < delayBeforeFire || weaponLocked) return;
        fireTimer = fireCooldown;
        currentAmmo--;

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
    }

    public override void ReloadWeapon()
    {
        if (weaponLocked == true) return;

        weaponLocked = true;
        Tween.Delay(reloadTime, () => 
        {
            currentAmmo = maxAmmo; 
            weaponLocked = false; 
        });
    }

    public override void OnWeaponSwapped()
    {
        fireTimer = 0.5f;
        spinningTime = 0f;
    }

    public override void DoWhileHolding()
    {
        float dt = Time.deltaTime;
        if (!firedLastFrame)
            spinningTime -= dt * cooldownMultiplier;
        else
            spinningTime += dt;

        spinningTime = Mathf.Clamp(spinningTime, 0f, delayBeforeFire);

        float t = spinningTime / delayBeforeFire;
        float easedT = spinCurve.Evaluate(t);
        minigunBarrels.localRotation *= Quaternion.Euler(0, easedT * barrelsSpinSpeed * dt, 0);

        firedLastFrame = false;
    }
}