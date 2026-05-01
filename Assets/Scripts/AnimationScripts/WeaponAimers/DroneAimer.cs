using PrimeTween;
using UnityEngine;

/*Aimer implementation for the drone's missile launcher.
Supports aiming towards the target and firing pooled missiles*/
public class DroneAimer : WeaponAimer
{
    [SerializeField] private Transform droneBodyBone;
    [SerializeField] private Transform weaponBone1; // sideways aiming
    [SerializeField] private Transform weaponBone2; // up/down aiming
    [SerializeField] private Transform weaponBone3End; // spawn missiles from here
    [SerializeField] private GameObject missilePrefab;
    public float missileLaunchSpeed = 120f;
    public float missileForwardAcceleration = 0f;
    public float missileGravityMultiplier = 0f;
    public float liftMissileLaunchAngle = 0f;
    public Vector3 rotationOnSpawn = new Vector3(-90, 0, 90); // rotate spawned missiles
    public float missileExplosionScale = 0.7f;
    public float minSqrDistanceToAim = 400f;
    public float maxAimDown = 40f; // max degrees up/down for aiming

    private Quaternion bodyRestRotation;
    private Quaternion weaponBone1RestRot;
    private Quaternion weaponBone2RestRot;
    private bool reloading = false;
    private Vector3 lastPos;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation = droneBodyBone.localRotation;
        weaponBone1RestRot = weaponBone1.localRotation;
        weaponBone2RestRot = weaponBone2.localRotation;
        lastPos = transform.position;
    }

    void LateUpdate()
    {
        lastPos = transform.position;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toTarget = worldTarget - transform.position;
        float targetY = toTarget.y;
        toTarget.y = 0f; // flatten to horizontal only for sideways aiming

        if (toTarget.sqrMagnitude < minSqrDistanceToAim) return;

        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        weaponBone1.rotation = Quaternion.Euler(0f, yaw + 180f, 0f) * weaponBone1RestRot;

        float pitch = Mathf.Atan2(targetY, toTarget.magnitude) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -maxAimDown, maxAimDown);
        weaponBone2.localRotation = weaponBone2RestRot * Quaternion.Euler(0f, 0f, pitch);
    }

    public override void TryFire(Vector3 targetPosition)
    {
        if (reloading) return;
        if (currentAmmo <= 0)
        {
            ReloadWeapon();
            return;
        }
        if (fireTimer > 0) return;

        Vector3 toTarget = targetPosition - transform.position;
        float sqrDistance = toTarget.magnitude * toTarget.magnitude;
        if (sqrDistance < minSqrDistanceToAim) return;

        fireTimer = fireCooldown;
        Vector3 droneVelocity = (transform.position - lastPos) / Time.deltaTime;

        SpawnMissile(targetPosition, droneVelocity);
        currentAmmo--;
    }

    public override void ReloadWeapon()
    {
        reloading = true;
        Tween.Delay(target: this, duration: reloadTime)
            .OnComplete(() => 
                {currentAmmo = maxAmmo;
                reloading = false;},
                warnIfTargetDestroyed: false);
    }

    public override void OnWeaponSwapped(){}

    /*Spawn a missile from the tip of the drone's launcher towards target*/
    private void SpawnMissile(Vector3 targetPosition, Vector3 droneVel)
    {
        Vector3 startPos = weaponBone3End.position;
        Quaternion startRot = weaponBone3End.rotation * Quaternion.Euler(rotationOnSpawn);

        Vector3 toTarget = (targetPosition - startPos).normalized;
        Quaternion toTargetRot = Quaternion.LookRotation(toTarget, weaponBone2.up);
        float dot = Vector3.Dot(startRot * Vector3.forward, toTarget);
        float t = Mathf.Clamp01(dot);

        Quaternion spawnRot = Quaternion.AngleAxis(liftMissileLaunchAngle, weaponBone2.right) * Quaternion.Slerp(startRot, toTargetRot, t);

        // Clamp by working on the direction vector instead of euler angles
        Vector3 launchDir = spawnRot * Vector3.forward;
        Vector3 launchDirFlat = new Vector3(launchDir.x, 0f, launchDir.z).normalized;
        float pitchAngle = Mathf.Atan2(-launchDir.y, new Vector2(launchDir.x, launchDir.z).magnitude) * Mathf.Rad2Deg;
        pitchAngle = Mathf.Clamp(pitchAngle, -maxAimDown, maxAimDown);

        // Rebuild direction from clamped pitch and original horizontal direction
        Vector3 clampedDir = Quaternion.AngleAxis(-pitchAngle, Vector3.Cross(launchDirFlat, Vector3.up)) * launchDirFlat;
        spawnRot = Quaternion.LookRotation(clampedDir, weaponBone2.up);

        GameObject missile = Instantiate(missilePrefab, startPos, spawnRot);
        missile.GetComponent<Missile>().Launch(targetPosition, missileLaunchSpeed, missileForwardAcceleration, missileGravityMultiplier, missileExplosionScale, droneVel);
    }

    public override void DoWhileHolding()
    {
        fireTimer -= Time.deltaTime;
    }

    public override bool ChoiceToFire(float lastFiringTime)
    {
        return true;
    }
}