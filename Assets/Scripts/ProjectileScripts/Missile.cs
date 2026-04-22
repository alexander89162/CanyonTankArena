using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Missile : MonoBehaviour
{
    public float lifeTime = 10f;
    private Vector3 currentVelocity;
    private float gravityMultiplier = 9.81f;
    private float forwardAcceleration;
    private float lifetimeRemaining = -1f;

    void Update()
    {
        if (lifetimeRemaining == -1) return;
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f) { Destroy(gameObject); return; }

        // 1) Handle movement
        currentVelocity += currentVelocity.normalized * forwardAcceleration * Time.deltaTime;
        currentVelocity += Vector3.down * gravityMultiplier * Time.deltaTime;
        transform.position += currentVelocity * Time.deltaTime;

        // 2) adjust orientation to match movement
        transform.rotation = Quaternion.LookRotation(currentVelocity);
    }

    public void Launch(Vector3 targetPosition, 
        float missileLaunchSpeed, float missileForwardAcceleration, 
        float missileGravityMultiplier)
    {
        gravityMultiplier = missileGravityMultiplier;
        forwardAcceleration = missileForwardAcceleration;

        currentVelocity = transform.forward * missileLaunchSpeed;
        lifetimeRemaining = lifeTime;
    }
}