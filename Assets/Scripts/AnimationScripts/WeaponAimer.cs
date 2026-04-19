using UnityEngine;
using PrimeTween;

public abstract class WeaponAimer : MonoBehaviour
{
    public bool weaponEnabled = false;
    public Renderer meshRenderer;
    public Transform weaponRootBone;
    private Vector3 originalScale;
    public int maxAmmo;
    public int currentAmmo;
    public float reloadTime;
    public float fireCooldown;

    protected virtual void Awake()
    {
        originalScale = weaponRootBone.localScale;
        if (weaponRootBone == null) Debug.LogError($"Root bone not assigned for {name}");
    }

    public virtual Tween ShowWeapon(float duration)
    {
        weaponRootBone.transform.localScale = Vector3.one * 0.01f;
        meshRenderer.enabled = true;
        return Tween.Scale(weaponRootBone.transform, originalScale, duration, Ease.OutQuad);
    }
    public virtual Tween HideWeapon(float duration)
    {
        return Tween.Scale(weaponRootBone.transform, Vector3.one * 0.01f, duration, Ease.InQuad)
            .OnComplete(this, target => target.meshRenderer.enabled = false);
    }
    public abstract void ReloadWeapon();
    public abstract void AimAt(Vector3 worldTarget);
    public abstract void TryFire(); // each aimer is responsible for spawning its own projectiles
    public virtual void OnWeaponSwapped() { }
}
