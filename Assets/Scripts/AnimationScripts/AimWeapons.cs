using UnityEngine;

public class AimWeapons : MonoBehaviour
{
    public float swapDuration = 0.7f;
    private WeaponState currentState;
    private WeaponAimer[] aimers;
    private int activeWeaponIndex = 0;
    private Transform target;
    public enum WeaponState {Holding, Swapping, Reloading}

    void Awake()
    {
        aimers = GetComponents<WeaponAimer>();
        target = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        switch (currentState)
        {
            case WeaponState.Swapping: return;
            case WeaponState.Holding:
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