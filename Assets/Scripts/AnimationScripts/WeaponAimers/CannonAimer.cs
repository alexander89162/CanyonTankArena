using UnityEngine;

public class CannonAimer : WeaponAimer
{
    [SerializeField] private Transform cannonBody;
    [SerializeField] private Transform cannonBarrel;

    private Quaternion bodyRestRotation;
    private Quaternion barrelRestRotation;

    protected override void Awake()
    {
        base.Awake();
        bodyRestRotation   = cannonBody.rotation;
        barrelRestRotation = cannonBarrel.rotation;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        Vector3 toEnemy = worldTarget - transform.position;

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(toEnemy.y, new Vector2(toEnemy.x, toEnemy.z).magnitude) * Mathf.Rad2Deg;

        cannonBody.rotation   = bodyRestRotation   * Quaternion.Euler(0, yaw, 0);
        cannonBarrel.localRotation = barrelRestRotation * Quaternion.Euler(pitch, 0, 0);
    }

    public override void Fire()
    {
        throw new System.NotImplementedException();
    }

    public override void ReloadWeapon(){} // do nothing for now
}