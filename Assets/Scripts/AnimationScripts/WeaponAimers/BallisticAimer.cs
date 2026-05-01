using UnityEngine;
using PrimeTween;

/*Aimer implementation for ballistic missile launcher.
Supports aiming towards the target and hiding/showing missiles*/
public class BallisticAimer : WeaponAimer
{
    [SerializeField] private Transform ballisticBody;
    [SerializeField] private Transform[] missiles;
    [SerializeField] private GameObject missilePrefab;
    public float missileReloadTime = 3.5f;
    public float missileLaunchSpeed = 120f;
    public float missileForwardAcceleration = 3f;
    public float missileGravityMultiplier = 120f;
    public float missileHideBackwardsOffset = 0.04f;
    public float liftMissileLaunchAngle = -50f;
    public Vector3 rotationOnSpawn = new Vector3(-90, 0, 0);
    public float missileExplosionScale = 1f;

    private Quaternion bodyRestRotation;
    private Vector3[] missileRestPositions;
    private float[] missileReloadTimers;
    private bool[] missileLoaded;
    private Vector3 lastPos;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation   = ballisticBody.localRotation;
        missileLoaded = new bool[missiles.Length];
        missileRestPositions = new Vector3[missiles.Length];
        missileReloadTimers = new float[missiles.Length];
        for (int i = 0; i < missileLoaded.Length; i++)
        {
            missileLoaded[i] = true;
            missileRestPositions[i] = missiles[i].localPosition;
        }
        lastPos = transform.position;
    }

    void LateUpdate()
    {
        lastPos = transform.position;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;

        ballisticBody.localRotation = bodyRestRotation * Quaternion.Euler(0, yaw, 0);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (fireTimer > 0 || currentAmmo <= 0) return;
        fireTimer = fireCooldown;
        
        for (int i = 0; i < missiles.Length; i++)
        {
            if (!missileLoaded[i]) continue;

            missileLoaded[i] = false;
            currentAmmo--;
            SpawnMissile(i, targetPosition);
            missiles[i].localPosition = missileRestPositions[i] - missiles[i].localRotation * Vector3.up * missileHideBackwardsOffset;

            return;
        }
    }

    // this weapon uses passive reload, avoid using this unless forced full reload
    public override void ReloadWeapon()
    {
        for (int i = 0; i < missiles.Length; i++)
        {
            if (missileLoaded[i]) continue;
            missileReloadTimers[i] = missileReloadTime; // force timer to trigger next frame
        }
    }

    public override void OnWeaponSwapped()
    {
        fireCooldown = 0.5f;
    }

    /*Spawn a missile and use index to spawn in correct start position*/
    private void SpawnMissile(int i, Vector3 targetPosition)
    {
        Vector3 startPos = missiles[i].position;
        Quaternion startRot = missiles[i].rotation * Quaternion.Euler(rotationOnSpawn);

        Vector3 toTarget = (targetPosition - startPos).normalized;
        Quaternion toTargetRot = Quaternion.LookRotation(toTarget, ballisticBody.up);
        float dot = Vector3.Dot(startRot * Vector3.forward, toTarget);
        float t = Mathf.Clamp01(dot);

        Quaternion spawnRot = Quaternion.AngleAxis(liftMissileLaunchAngle, ballisticBody.right) * Quaternion.Slerp(startRot, toTargetRot, t);

        Vector3 currentVelocity = (transform.position - lastPos) / Time.deltaTime;
        GameObject missile = Instantiate(missilePrefab, startPos, spawnRot);
        missile.GetComponent<Missile>().Launch(targetPosition, missileLaunchSpeed, missileForwardAcceleration, missileGravityMultiplier, missileExplosionScale, currentVelocity);
    }

    public override void DoWhileHolding()
    {
        for (int i = 0; i < missiles.Length; i++)
        {
            if (missileLoaded[i]) continue;

            missileReloadTimers[i] += Time.deltaTime;
            if (missileReloadTimers[i] >= missileReloadTime)
            {
                missileReloadTimers[i] = 0f;
                ReloadSingleMissile(i);
            }
        }
    }

    private void ReloadSingleMissile(int index)
    {
        Tween.LocalPosition(missiles[index], missileRestPositions[index], duration: 0.3f, Ease.OutQuad)
        .OnComplete(() =>
        {
            missileLoaded[index] = true;
            currentAmmo++;
        }, warnIfTargetDestroyed: false);
    }

    public override bool ChoiceToFire(float lastFiringTime)
    {
        float timeSinceLastFire = Time.time - lastFiringTime;
        float normalized = timeSinceLastFire / idealTimeBetweenFires;
        return Random.value < normalized * 0.01f;
    }
}