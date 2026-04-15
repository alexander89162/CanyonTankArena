using UnityEngine;

/*Aimer implementation for the basic cannon
Supports aiming at the target and clamping cannon-body at clampLiftCannon,
adding any remaining up/down aim via cannon-barrel, up until clampLiftBarrel*/
public class CannonAimer : WeaponAimer
{
    [SerializeField] private Transform cannonBody;
    [SerializeField] private Transform cannonBarrel;
    [SerializeField] private float clampLiftCannon;
    [SerializeField] private float clampLiftBarrel;
    
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

        Vector3 toEnemy = worldTarget - transform.position;

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(toEnemy.y, new Vector2(toEnemy.x, toEnemy.z).magnitude) * Mathf.Rad2Deg;

        float cannonBodyPitch = Mathf.Clamp(pitch, -clampLiftCannon, clampLiftCannon);
        float barrelPitch = Mathf.Clamp(pitch - cannonBodyPitch, -clampLiftBarrel, clampLiftBarrel);

        cannonBody.rotation = bodyRestRotation * Quaternion.Euler(-cannonBodyPitch, yaw, 0);
        cannonBarrel.localRotation = barrelRestRotation * Quaternion.Euler(barrelPitch, 0, 0);
    }

    public override void Fire()
    {
        Debug.Log("Fire() was called"); // TODO
    }

    public override void ReloadWeapon(){} // do nothing for now
}