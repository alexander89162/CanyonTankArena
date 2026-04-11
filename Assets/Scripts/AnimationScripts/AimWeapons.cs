using UnityEngine;

public class AimWeapons : MonoBehaviour
{
    private WeaponState currentState;
    private WeaponAimer[] aimers;
    private int activeWeaponIndex = 0;
    private Transform target;
    private enum WeaponState {Holding, Swapping, Reloading}

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
}