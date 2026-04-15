using UnityEngine;

public class AimWeapons : MonoBehaviour
{
    public TargetingSystem targetingSystem;
    public float swapDuration = 0.7f;
    private WeaponState currentState;
    private WeaponAimer[] aimers;
    private int activeWeaponIndex = 0;
    private Transform target;
    public enum WeaponState {Holding, Swapping, Reloading}
    public bool playerControlled = false;

    void Awake()
    {
        aimers = GetComponents<WeaponAimer>();
        if (playerControlled) targetingSystem = GetComponent<TargetingSystem>();
        target = GameObject.FindWithTag("Player").transform;
    }

    void LateUpdate()
    {
        switch (currentState)
        {
            case WeaponState.Swapping: return;
            case WeaponState.Holding:
                if (playerControlled)
                {
                    aimers[activeWeaponIndex].AimAt(GetTarget());
                }
                else
                    aimers[activeWeaponIndex].AimAt(target.position);
                return;
        }
    }

    public void SwapToWeapon(int newWeaponIndex)
    {
        SetState(WeaponState.Swapping);
        aimers[activeWeaponIndex].HideWeapon(swapDuration / 2)
            .OnComplete(() => {
                aimers[newWeaponIndex].ShowWeapon(swapDuration / 2)
                    .OnComplete(() => {
                        activeWeaponIndex = newWeaponIndex;
                        SetState(WeaponState.Holding);
                    });
            });
    }

    public Vector3 GetTarget()
    {
        return targetingSystem.GetTargetPosition();
    }

    public void SetState(WeaponState newState)
    {
        switch (newState)
        {
            case WeaponState.Holding: break;
            case WeaponState.Reloading: break;
            case WeaponState.Swapping: break;
        }
    }
}