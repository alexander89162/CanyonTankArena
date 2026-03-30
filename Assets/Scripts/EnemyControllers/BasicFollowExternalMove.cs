using UnityEngine;
using UnityEngine.AI;

public class BasicFollowExternalMove : MonoBehaviour
{
    public Vector3 targetPos;
    public Transform target;
    private NavMeshAgent agent;
    private MovementController movementController;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<MovementController>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        targetPos = transform.position;
    }

    void Update()
    {
        targetPos = target.position;
        agent.SetDestination(targetPos);
        Vector3 desired = agent.desiredVelocity;

        // Convert world-space NavMesh direction to local tank input
        Vector3 localDir = transform.InverseTransformDirection(desired);
        movementController.moveInput = new Vector2(localDir.x, localDir.z).normalized;
    }
}