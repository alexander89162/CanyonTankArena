using UnityEngine;

/*Aimer implementation for the drone's missile launcher.
Supports aiming towards the target and firing pooled missiles*/
public class DroneAimer : WeaponAimer
{
    [SerializeField] private Transform droneBody;
    [SerializeField] private Transform spinX;
    [SerializeField] private Transform spinZ;
    [SerializeField] private GameObject missilePrefab;
    public float missileReloadTime = 3.5f;
    public float missileLaunchSpeed = 120f;
    public float missileForwardAcceleration = 3f;
    public float missileGravityMultiplier = 0f;
    public float liftMissileLaunchAngle = 0f;
    public Vector3 rotationOnSpawn = new Vector3(-90, 0, 0);
    public float missileExplosionScale = 0.7f;

    private Quaternion bodyRestRotation;
    private float missileReloadTimer;

    protected override void Awake()
    {
        base.Awake();
        // bodyRestRotation   = ballisticBody.localRotation;
        // missileLoaded = new bool[missiles.Length];
        // missileRestPositions = new Vector3[missiles.Length];
        // missileReloadTimers = new float[missiles.Length];
        // for (int i = 0; i < missileLoaded.Length; i++)
        // {
        //     missileLoaded[i] = true;
        //     missileRestPositions[i] = missiles[i].localPosition;
        // }
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;

        // ballisticBody.localRotation   = bodyRestRotation   * Quaternion.Euler(0, yaw, 0);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (fireTimer > 0 || currentAmmo <= 0) return;
        fireTimer = fireCooldown;
        
        //
    }

    // this weapon uses passive reload, avoid using this unless forced full reload
    public override void ReloadWeapon()
    {
        //
    }

    public override void OnWeaponSwapped()
    {
        fireCooldown = 0.5f;
    }

    /*Spawn a missile and use index to spawn in correct start position*/
    private void SpawnMissile(int i, Vector3 targetPosition)
    {
        // Vector3 startPos = missiles[i].position;
        // Quaternion startRot = missiles[i].rotation * Quaternion.Euler(rotationOnSpawn);

        // Vector3 toTarget = (targetPosition - startPos).normalized;
        // Quaternion toTargetRot = Quaternion.LookRotation(toTarget, ballisticBody.up);
        // float dot = Vector3.Dot(startRot * Vector3.forward, toTarget);
        // float t = Mathf.Clamp01(dot);

        // Quaternion spawnRot = Quaternion.AngleAxis(liftMissileLaunchAngle, ballisticBody.right) * Quaternion.Slerp(startRot, toTargetRot, t);

        // GameObject missile = Instantiate(missilePrefab, startPos, spawnRot);
        // missile.GetComponent<Missile>().Launch(targetPosition, missileLaunchSpeed, missileForwardAcceleration, missileGravityMultiplier, missileExplosionScale);
    }

    public override void DoWhileHolding()
    {
        //
    }
}