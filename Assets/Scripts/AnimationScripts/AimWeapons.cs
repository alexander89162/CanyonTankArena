using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

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
            case WeaponState.Swapping: return;
            case WeaponState.Holding:
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
                return;
        }
    }

    public void SwapToWeapon(int newWeaponIndex)
    {
        SetState(WeaponState.Swapping);
        Sequence.Create()
            .Chain(aimers[activeWeaponIndex].HideWeapon(swapDuration / 2))
            .ChainCallback(() =>
            {
                Sequence.Create()
                    .Chain(aimers[newWeaponIndex].ShowWeapon(swapDuration / 2))
                    .ChainCallback(() =>
                    {
                        activeWeaponIndex = newWeaponIndex;
                        aimers[activeWeaponIndex].weaponEnabled = true;
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