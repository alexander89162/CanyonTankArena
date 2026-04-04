using UnityEngine;

public class TankSlopeForRig : MonoBehaviour
{
    [Header("Slope Settings")]
    public float alignSpeed = 5f;
    public float alignRefreshRate = 0.05f;
    public float rayDistance = 1.5f;
    public LayerMask groundLayer;
    public float minMoveSpeedToAlign = 0.2f;
    public bool debug = false;

    [Header("References")]
    public Transform tankRoot; // Root is used for raycasting
    public Transform tankBodyBone;
    private Transform boneParent;

    private float alignRefreshTimer = 0f;

    void Awake()
    {
        #if UNITY_EDITOR
        if (debug) Debug.Log("Awake was called in TankSlopeForRig");
        #endif

        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Ground");
        if (tankBodyBone != null) boneParent = tankBodyBone.parent;

        alignRefreshTimer = Random.Range(0, alignRefreshRate); // offset timer so tanks align at different frames (load balance optimization)
    }

    void Update()
    {
        alignRefreshTimer += Time.deltaTime;
    }

    // Call this method every frame in controller to update alignment
    public void UpdateAlignment(Vector3 currentVelocity)
    {
        if (alignRefreshTimer < alignRefreshRate) return;

        alignRefreshTimer = 0f;

        if (currentVelocity.sqrMagnitude < minMoveSpeedToAlign)
        {
            #if UNITY_EDITOR
            if (debug) Debug.Log($"Skipped AlignToSlope; velocity below minMoveSpeedToAlign");
            #endif

            return;
        }

        AlignToSlope();
    }

    void AlignToSlope()
    {
        #if UNITY_EDITOR
        if (debug) Debug.Log($"AlignToSlope was called.");
        #endif

        Vector3 rayStart = tankRoot.position + Vector3.up * 1f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            if (debug) Debug.Log($"The raycast hit at {hit.point}");
            Vector3 groundNormal = hit.normal;
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(tankRoot.forward, groundNormal).normalized;

            // World-space target rotation for the bone
            Quaternion worldTarget = Quaternion.LookRotation(forwardOnSlope, groundNormal);

            // Convert to local space relative to the bone's parent
            Quaternion localTarget = Quaternion.Inverse(boneParent.rotation) * worldTarget;

            // Slerp from current local rotation toward the target
            tankBodyBone.localRotation = Quaternion.Slerp(
                tankBodyBone.localRotation,
                localTarget,
                alignSpeed * Time.deltaTime
            );

            #if UNITY_EDITOR
            if (debug) Debug.Log($"localRotation of tankBodyBone: {tankBodyBone.localRotation}, localTarget = {localTarget}");
            #endif
        }
    }

    void OnDrawGizmosSelected()
    {
        if (tankRoot == null) return;
        Gizmos.color = Color.yellow;
        Vector3 start = tankRoot.position + Vector3.up * 0.5f;
        Gizmos.DrawRay(start, Vector3.down * rayDistance);
    }
}