using UnityEngine;

public class Missile : MonoBehaviour
{
    private Vector3 currentVelocity;
    private float gravityMultiplier = 9.81f;

    void Update()
    {
        // 1) Handle movement
        currentVelocity += Vector3.down * gravityMultiplier * Time.deltaTime;
        transform.position += currentVelocity * Time.deltaTime;

        // 2) adjust orientation to match movement
        transform.rotation = Quaternion.LookRotation(currentVelocity);
    }

    public void Launch(Vector3 targetPosition, float missileGravityMultiplier)
    {
        gravityMultiplier = missileGravityMultiplier;
        Vector3 toTarget = targetPosition - transform.position;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        
        float distance = toTargetXZ.magnitude;
        float heightDiff = toTarget.y;
        float angle = 45f * Mathf.Deg2Rad; // launch angle, can be serialized

        float v0 = Mathf.Sqrt(gravityMultiplier * distance * distance /
                (2f * Mathf.Cos(angle) * Mathf.Cos(angle) *
                (distance * Mathf.Tan(angle) - heightDiff)));

        currentVelocity = toTargetXZ.normalized * v0 * Mathf.Cos(angle)
                        + Vector3.up * v0 * Mathf.Sin(angle);
    }
}