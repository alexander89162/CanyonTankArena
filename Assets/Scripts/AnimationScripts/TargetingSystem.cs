using UnityEngine;

/// <summary>
/// Handles raycasting for 3rd person targeting.
/// 1. Casts from camera forward to find nominal target
/// 2. Casts from weapon to that target to check line-of-sight
/// 3. Provides actual valid target position
/// </summary>
public class TargetingSystem : MonoBehaviour
{
    [Tooltip("Main camera (auto-found if not assigned)")]
    [SerializeField] private Camera mainCamera;
    
    [Tooltip("Weapon/cannon transform for line-of-sight check")]
    [SerializeField] private Transform weaponTransform;
    
    [Tooltip("Layer mask for camera raycast (what can be targeted)")]
    [SerializeField] private LayerMask targetLayerMask = -1;
    
    [Tooltip("Layer mask for weapon LOS check (what blocks shots)")]
    [SerializeField] private LayerMask losBlockerMask = -1;
    
    [Tooltip("Max distance camera can see")]
    [SerializeField] private float maxCameraDistance = 1000f;
    
    [Tooltip("Ignore player tank layer when raycasting")]
    [SerializeField] private LayerMask ignoreLayerMask = 0;

    [Tooltip("If true, target comes from weapon/muzzle forward ray instead of camera-forward ray")]
    [SerializeField] private bool useWeaponForwardAsPrimary = true;

    private AimController aimController;
    
    // Current target info
    private Vector3 cameraTarget;
    private Vector3 actualTarget;
    private bool hasValidTarget;

    void Start()
    {
        ResolveReferences();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateTargeting();
    }

    void UpdateTargeting()
    {
        ResolveReferences();

        int cameraMask = targetLayerMask & ~ignoreLayerMask.value;
        int losMask = losBlockerMask & ~ignoreLayerMask.value;

        // Optional mode: weapon/muzzle line-of-sight is the primary target source
        if (useWeaponForwardAsPrimary && weaponTransform != null)
        {
            Vector3 weaponRayOrigin = weaponTransform.position;
            Vector3 weaponRayDirection = GetWeaponAimDirection();

            if (Physics.Raycast(weaponRayOrigin, weaponRayDirection, out RaycastHit weaponHit, maxCameraDistance, losMask))
            {
                actualTarget = weaponHit.point;
            }
            else
            {
                actualTarget = weaponRayOrigin + weaponRayDirection * maxCameraDistance;
            }

            cameraTarget = actualTarget;
            hasValidTarget = true;
            return;
        }

        if (mainCamera == null)
        {
            hasValidTarget = false;
            return;
        }

        // 1. Raycast from camera forward
        Vector3 cameraRayOrigin = mainCamera.transform.position;
        Vector3 cameraRayDirection = mainCamera.transform.forward;

        if (Physics.Raycast(cameraRayOrigin, cameraRayDirection, out RaycastHit cameraHit, maxCameraDistance, cameraMask))
        {
            cameraTarget = cameraHit.point;
        }
        else
        {
            // No hit - use max distance point
            cameraTarget = cameraRayOrigin + cameraRayDirection * maxCameraDistance;
        }

        // 2. Raycast from weapon to camera target (line-of-sight check)
        if (weaponTransform != null)
        {
            Vector3 weaponToTarget = (cameraTarget - weaponTransform.position).normalized;
            float distanceToTarget = Vector3.Distance(weaponTransform.position, cameraTarget);

            if (Physics.Raycast(weaponTransform.position, weaponToTarget, out RaycastHit losHit, distanceToTarget, losMask))
            {
                // Hit something between weapon and camera target - use that as actual target
                actualTarget = losHit.point;
                hasValidTarget = true;
            }
            else
            {
                // Clear line of sight to camera target
                actualTarget = cameraTarget;
                hasValidTarget = true;
            }
        }
        else
        {
            // No weapon transform - just use camera target
            actualTarget = cameraTarget;
            hasValidTarget = true;
        }
    }

    void ResolveReferences()
    {
        if (weaponTransform != null)
            return;

        if (aimController == null)
        {
            aimController = GetComponentInParent<AimController>();

            if (aimController == null)
                aimController = FindFirstObjectByType<AimController>();
        }

        if (aimController != null)
        {
            weaponTransform = aimController.GetCannonTransform();

            if (weaponTransform == null)
                weaponTransform = aimController.GetTurretTransform();
        }
    }

    Vector3 GetWeaponAimDirection()
    {
        if (aimController != null)
        {
            Vector3 direction = aimController.GetAimDirection();
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        return weaponTransform.forward;
    }

    /// <summary>Get the current target position that weapon should aim at</summary>
    public Vector3 GetTargetPosition()
    {
        return actualTarget;
    }

    /// <summary>Get the raw camera-forward target (before LOS check)</summary>
    public Vector3 GetCameraTarget()
    {
        return cameraTarget;
    }

    /// <summary>Returns true if a valid target exists</summary>
    public bool HasValidTarget()
    {
        return hasValidTarget;
    }

    /// <summary>Debug visualization</summary>
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        // Camera raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * maxCameraDistance);

        // Weapon to target
        if (weaponTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(weaponTransform.position, cameraTarget);
        }
    }
}
