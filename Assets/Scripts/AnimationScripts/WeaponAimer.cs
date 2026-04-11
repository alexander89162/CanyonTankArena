using UnityEngine;
using PrimeTween;

public abstract class WeaponAimer : MonoBehaviour
{
    public bool weaponEnabled;
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private float swapDuration = 0.7f;
    private Vector3 originalScale;
    public int maxAmmo;
    public int currentAmmo;

    protected virtual void Awake()
    {
        originalScale = transform.localScale;
    }

    public virtual void ShowWeapon()
    {
        meshRenderer.enabled = true;
        Tween.Scale(transform, originalScale, swapDuration, Ease.OutQuad);
    }
    public virtual void HideWeapon()
    {
        Tween.Scale(transform, Vector3.one * 0.01f, swapDuration, Ease.InQuad)
            .OnComplete(this, target => target.meshRenderer.enabled = false);
    }
    public abstract void ReloadWeapon();
    public abstract void AimAt(Vector3 worldTarget);
    public abstract void Fire();
}
