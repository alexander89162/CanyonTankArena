using UnityEngine;
using PrimeTween;

public abstract class WeaponAimer : MonoBehaviour
{
    public bool weaponEnabled = false;
    public Renderer meshRenderer;
    private Vector3 originalScale;
    public int maxAmmo;
    public int currentAmmo;
    public float reloadTime;

    protected virtual void Awake()
    {
        originalScale = meshRenderer.transform.localScale;
    }

    public virtual Tween ShowWeapon(float duration)
    {
        meshRenderer.transform.localScale = Vector3.one * 0.01f;
        meshRenderer.enabled = true;
        return Tween.Scale(meshRenderer.transform, originalScale, duration, Ease.OutQuad);
    }
    public virtual Tween HideWeapon(float duration)
    {
        return Tween.Scale(meshRenderer.transform, Vector3.one * 0.01f, duration, Ease.InQuad)
            .OnComplete(this, target => target.meshRenderer.enabled = false);
    }
    public abstract void ReloadWeapon();
    public abstract void AimAt(Vector3 worldTarget);
    public abstract void Fire();
}
