using UnityEngine;
using PrimeTween;

/*Aimer implementation for ballistic missile launcher.
Supports aiming towards the target and hiding/showing missiles*/
public class BallisticAimer : WeaponAimer
{
    [SerializeField] private Transform ballisticBody;
    [SerializeField] private Transform[] missiles;
    [SerializeField] private GameObject missilePrefab;
    public float missileGravityMultiplier = 15f;
    public float missileHideBackwardsOffset = 0.04f;

    private Quaternion bodyRestRotation;
    private Vector3[] missileRestPositions;
    private bool[] missileLoaded;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation   = ballisticBody.localRotation;
        missileLoaded = new bool[missiles.Length];
        missileRestPositions = new Vector3[missiles.Length];
        for (int i = 0; i < missileLoaded.Length; i++)
        {
            missileLoaded[i] = true;
            missileRestPositions[i] = missiles[i].localPosition;
        }
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemyWorld = worldTarget - transform.position;
        Vector3 toEnemy = transform.InverseTransformDirection(toEnemyWorld);

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;

        ballisticBody.localRotation   = bodyRestRotation   * Quaternion.Euler(0, yaw, 0);
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

    public override void ReloadWeapon()
    {
        for (int i = 0; i < missiles.Length; i++)
        {
            if (missileLoaded[i]) continue;

            Tween.LocalPosition(missiles[i], missileRestPositions[i], duration: 0.3f, startDelay: i * 1f)
                .OnComplete(() =>
                {
                    missileLoaded[i] = true;
                    currentAmmo++;
                });

            missileLoaded[i] = true;
            currentAmmo++;
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
        Quaternion startRot = missiles[i].rotation;

        GameObject missile = Instantiate(missilePrefab, startPos, startRot);
        missile.GetComponent<Missile>().Launch(targetPosition, missileGravityMultiplier);
    }

    public override void DoWhileHolding()
    {
        //
    }
}