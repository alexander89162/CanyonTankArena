using UnityEngine;

/*Aimer implementation for ballistic missile launcher.
Supports aiming towards the target and hiding/showing missiles*/
public class BallisticAimer : WeaponAimer
{
    [SerializeField] private Transform ballisticBody;
    [SerializeField] private Transform[] missiles;

    private Quaternion bodyRestRotation;
    private bool[] missileLoaded;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation   = ballisticBody.rotation;
        missileLoaded = new bool[missiles.Length];
        for (int i = 0; i < missileLoaded.Length; i++)
            missileLoaded[i] = true;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemy = worldTarget - transform.position;

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;

        ballisticBody.rotation   = bodyRestRotation   * Quaternion.Euler(0, yaw, 0);
    }

    public override void Fire()
    {
        if (currentAmmo <= 0) return;
        
        for (int i = 0; i < missiles.Length; i++)
        {
            if (!missileLoaded[i]) continue;

            missileLoaded[i] = false;
            currentAmmo--;
            // PrimeTween move missile[i] backward in local space
            // spawn missile prefab
            return;
        }
    }

    public override void ReloadWeapon()
    {
        for (int i = 0; i < missiles.Length; i++)
        {
            if (missileLoaded[i]) continue;
            // PrimeTween move missile[i] back to original position over 4s
            // stagger each one with startDelay: i * 1f so they reload one at a time
            missileLoaded[i] = true;
            currentAmmo++;
        }
    }
}