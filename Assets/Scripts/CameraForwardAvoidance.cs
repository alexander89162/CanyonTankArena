using UnityEngine;
using Unity.Cinemachine;

public class CameraTerrainAvoidance : CinemachineExtension
{
    public LayerMask terrainLayer;
    public float cameraRadius = 0.3f;
    public float smoothSpeed = 10f;
    public float collisionBuffer = 0.05f;

    private float currentPush = 0f;

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body)
            return;

        Vector3 camPos = state.RawPosition;
        Vector3 targetPos = state.ReferenceLookAt;

        Vector3 dir = camPos - targetPos;
        float distance = dir.magnitude;

        if (distance < 0.001f)
            return;

        dir /= distance;

        float targetPush = 0f;

        if (Physics.SphereCast(targetPos, cameraRadius, dir, out RaycastHit hit, distance, terrainLayer))
        {
            float safeDistance = Mathf.Max(hit.distance - collisionBuffer, 0f);
            targetPush = distance - safeDistance;
        }

        // Frame-rate independent smoothing
        float t = 1f - Mathf.Exp(-smoothSpeed * deltaTime);
        currentPush = Mathf.Lerp(currentPush, targetPush, t);

        state.RawPosition -= dir * currentPush;
    }
}