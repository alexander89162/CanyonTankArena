using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimWeapons : MonoBehaviour
{
    public TargetingSystem targetingSystem;
    public float swapDuration = 0.3f;
    private WeaponState currentState;
    private WeaponAimer[] aimers;
    private int activeWeaponIndex = 0;
    private Transform target;
    public enum WeaponState {Holding, Swapping, Reloading}
    public bool playerControlled = false;
    private PlayerInput playerInput;
    private InputAction swapAction;

    void Awake()
    {
        aimers = GetComponents<WeaponAimer>();
        HideOtherWeapons(0); // hide all except index 0

        if (playerControlled) targetingSystem = GetComponent<TargetingSystem>();
        target = GameObject.FindWithTag("Player").transform;

        if (playerControlled)
        {
            playerInput = GetComponent<PlayerInput>();
            swapAction = playerInput.actions["SwapNextWeapon"];
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case WeaponState.Swapping: 
                AimCurrentWeapon();
                return;
            case WeaponState.Holding:
                AimCurrentWeapon();
                return;
        }
    }

    public void SwapToWeapon(int newWeaponIndex)
    {
        if (currentState == WeaponState.Swapping) return;

        SetState(WeaponState.Swapping);
        Sequence.Create()
            .Chain(aimers[activeWeaponIndex].HideWeapon(swapDuration / 2))
            .ChainCallback(() =>
            {
                // aimers[activeWeaponIndex].DisableRenderer();
                // aimers[newWeaponIndex].EnableRenderer();
                aimers[newWeaponIndex].weaponEnabled = true;
                activeWeaponIndex = newWeaponIndex;
                Sequence.Create()
                    .Chain(aimers[newWeaponIndex].ShowWeapon(swapDuration / 2))
                    .ChainCallback(() =>
                    {
                        SetState(WeaponState.Holding);
                    });
            });
    }

    /*Hide and disable all weapons except the index specified*/
    private void HideOtherWeapons(int index)
    {
        for (int i = 0; i < aimers.Length; i++)
        {
            if (i == index) continue;

            aimers[i].weaponEnabled = false;
            aimers[i].meshRenderer.enabled = false;
        }
    }

    private void AimCurrentWeapon()
    {
        if (playerControlled)
        {
            if (swapAction.WasPressedThisFrame())
            {
                int next = (activeWeaponIndex + 1) % aimers.Length;
                SwapToWeapon(next);
                return;
            }
            aimers[activeWeaponIndex].AimAt(GetTarget());
        }
        else
            aimers[activeWeaponIndex].AimAt(target.position);
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