using UnityEngine;
using UnityEngine.AI;

public class BasicFollow : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError("BasicFollow Error: the agent is null!");

        if (target == null) Debug.LogError("BasicFollow Error: The target is null!");
    }

    void Update()
    {
        Debug.Log($"Agent on NavMesh? - {agent.isOnNavMesh}");
        Debug.Log($"Agent has a valid path? {agent.hasPath}");
        Debug.Log($"Agent path status: {agent.pathStatus}");
        Debug.Log("Velocity: " + agent.velocity);
        Debug.Log($"Agent remaining distance: {agent.remainingDistance}");

        agent.SetDestination(target.position);
        transform.position = agent.nextPosition;
    }
}