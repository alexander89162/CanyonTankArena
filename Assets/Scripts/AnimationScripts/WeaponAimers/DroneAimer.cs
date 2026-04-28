using PrimeTween;
using UnityEngine;

/*Aimer implementation for the drone's missile launcher.
Supports aiming towards the target and firing pooled missiles*/
public class DroneAimer : WeaponAimer
{
    [SerializeField] private Transform droneBodyBone;
    [SerializeField] private Transform weaponBone1; // sideways aiming
    [SerializeField] private Transform weaponBone2; // up/down aiming
    [SerializeField] private GameObject missilePrefab;
    public float missileLaunchSpeed = 120f;
    public float missileForwardAcceleration = 0f;
    public float missileGravityMultiplier = 0f;
    public float liftMissileLaunchAngle = 0f;
    public Vector3 rotationOnSpawn = new Vector3(-90, 0, 0); // rotate spawned missiles
    public float missileExplosionScale = 0.7f;

    private Quaternion bodyRestRotation;
    private Quaternion weaponBone1RestRot;
    private Quaternion weaponBone2RestRot;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation = droneBodyBone.localRotation;
        weaponBone1RestRot = weaponBone1.localRotation;
        weaponBone2RestRot = weaponBone2.localRotation;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;

        weaponBone1.localRotation = weaponBone1RestRot * Quaternion.Euler(0, yaw, 0);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (currentAmmo <= 0) ReloadWeapon();
        if (fireTimer > 0) return;
        fireTimer = fireCooldown;
        
        SpawnMissile(targetPosition);
    }

    public override void ReloadWeapon()
    {
        Tween.Delay(duration: reloadTime)
            .OnComplete(() => currentAmmo = maxAmmo);
    }

    public override void OnWeaponSwapped()
    {
        fireCooldown = 0.5f;
    }

    /*Spawn a missile and use index to spawn in correct start position*/
    private void SpawnMissile(Vector3 targetPosition)
    {
        Vector3 startPos = weaponBone2.position;
        Quaternion startRot = weaponBone2.rotation * Quaternion.Euler(rotationOnSpawn);

        Vector3 toTarget = (targetPosition - startPos).normalized;
        Quaternion toTargetRot = Quaternion.LookRotation(toTarget, weaponBone2.up);
        float dot = Vector3.Dot(startRot * Vector3.forward, toTarget);
        float t = Mathf.Clamp01(dot);

        Quaternion spawnRot = Quaternion.AngleAxis(liftMissileLaunchAngle, weaponBone2.right) * Quaternion.Slerp(startRot, toTargetRot, t);

        GameObject missile = Instantiate(missilePrefab, startPos, spawnRot);
        missile.GetComponent<Missile>().Launch(targetPosition, missileLaunchSpeed, missileForwardAcceleration, missileGravityMultiplier, missileExplosionScale);
    }

    public override void DoWhileHolding()
    {
        fireTimer -= Time.deltaTime;
    }
}