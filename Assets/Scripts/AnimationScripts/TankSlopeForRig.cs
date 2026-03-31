using UnityEngine;

public class TankSlopeForRig : MonoBehaviour
{
    [Header("Slope Settings")]
    public float alignSpeed = 5f;
    public float rayDistance = 1.5f;
    public LayerMask groundLayer;
    public float minMoveSpeedToAlign = 0.2f;
    public bool debug = false;

    [Header("References")]
    public Transform tankRoot;       // Root: used for raycasting
    public Transform tankBodyBone;   // Drag the 'tank-body' bone here

    private Quaternion boneRestRotation;
    private Vector3 currentVelocity;

    void Awake()
    {
        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Ground");

        // Cache the bone's default local rotation so we can offset from it
        if (tankBodyBone != null)
            boneRestRotation = tankBodyBone.localRotation;
    }

    // Call this method to update alignment every frame in controller
    public void UpdateAlignment(Vector3 velocity)
    {
        currentVelocity = velocity;

        if (currentVelocity.magnitude < minMoveSpeedToAlign)
        {
            if (debug) Debug.Log($"Skipped AlignToSlope; velocity below minMoveSpeedToAlign");
            return;
        }

        AlignToSlope();
    }

    void AlignToSlope()
    {
        if (debug) Debug.Log($"AlignToSlope was called.");
        Vector3 rayStart = tankRoot.position + Vector3.up * 1f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            Vector3 groundNormal = hit.normal;
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(tankRoot.forward, groundNormal).normalized;

            // World-space target rotation for the bone
            Quaternion worldTarget = Quaternion.LookRotation(forwardOnSlope, groundNormal);

            // Convert to local space relative to the bone's parent
            Quaternion localTarget = Quaternion.Inverse(tankBodyBone.parent.rotation) * worldTarget;

            // Slerp from current local rotation toward the target
            tankBodyBone.localRotation = Quaternion.Slerp(
                tankBodyBone.localRotation,
                localTarget,
                alignSpeed * Time.deltaTime
            );
            if (debug) Debug.Log($"localRotation of tankBodyBone: {tankBodyBone.localRotation}, localTarget = {localTarget}");
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