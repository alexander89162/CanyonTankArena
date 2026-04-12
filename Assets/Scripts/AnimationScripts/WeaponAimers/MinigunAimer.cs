using UnityEngine;

/*Aimer implementation for minigun.
NOTE: this aimer has rig issues so offsets are hardcoded into the angle math*/
public class MinigunAimer : WeaponAimer
{
    [SerializeField] private Transform minigunBase;
    [SerializeField] private Transform minigunBody;
    [SerializeField] private Transform minigunBarrels;
    [SerializeField] private float lowerTarget = 4f; // make the gun point lower to better face the target

    private Quaternion baseRestRotation;
    private Quaternion bodyRestRotation;
    private Quaternion barrelsRestRotation;

    protected override void Awake()
    {
        base.Awake();
        baseRestRotation    = minigunBase.rotation;
        bodyRestRotation    = minigunBody.rotation;
        barrelsRestRotation = minigunBarrels.rotation;
    }

    public override void AimAt(Vector3 worldTarget)
    {
        if (!weaponEnabled) return;

        worldTarget.y -= lowerTarget;

        Vector3 toEnemy = worldTarget - transform.position;

        float yaw   = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(toEnemy.y, new Vector2(toEnemy.x, toEnemy.z).magnitude) * Mathf.Rad2Deg;

        minigunBase.rotation      = baseRestRotation * Quaternion.Euler(0, yaw - 90f, 0);
        minigunBody.localRotation = bodyRestRotation * Quaternion.Euler(pitch, 0, 0);
    }

    public override void Fire()
    {
        minigunBarrels.localRotation *= Quaternion.Euler(0, 20, 0); // temporary
    }

    public override void ReloadWeapon(){} // do nothing for now
}