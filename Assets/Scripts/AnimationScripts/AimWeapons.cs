using UnityEngine;

public class AimWeapons : MonoBehaviour
{
    private WeaponState currentState;
    private WeaponAimer[] aimers;
    private int activeWeaponIndex = 0;
    private Vector3 target;
    private enum WeaponState {NotSwapping, Swapping}

    void Awake()
    {
        aimers = GetComponents<WeaponAimer>();
    }

    void Update()
    {
        switch (currentState)
        {
            case WeaponState.Swapping: return;
            case WeaponState.NotSwapping:
                aimers[activeWeaponIndex].AimAt(target);
                return;
        }
    }
}