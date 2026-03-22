using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/*This script makes a unit circle an opponent once spotting them. Using probabilities, 
the unit explores the map in Wandering mode (slower movement and shorter vision 
range) until finding an opponent. There is support for multiple units on enemy and 
ally teams, and heuristics for which opponent to attack*/
public class AssaultGeneric : MonoBehaviour
{
    private enum UnitState
    {
        Wandering,
        Chasing,
        Avoiding
    }

    public GameObject[] opponents;
    public LayerMask opponentLayer;
    public Vector3[] waypoints; // defaults for target when no unit target is seen
    public float wanderingSpeed = 5f; // for when no opponents are detected
    public float chasingSpeed = 10f;
    public float detectionRange = 200f;
    public float playerBias = 0.5f; // -1 avoid player, 0 neutral, 0.5 tend to pick player more, 1 pick player always

    private NavMeshAgent agent;
    private UnitState currentState;
    private int currentWaypoint = 0;
    private Vector3 targetPos; // this unit will move towards this point
    private int unitTargetId; // the ID of the unit we want this unit to attack
    private float currentSpeed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, opponentLayer);
        if (hits.Length > 0)
        {
            // Optionally pick the closest one
            Transform closest = GetClosest(hits); // TODO: replace with smarter heuristic, and bias to pick player more often
            SetTargetPosition(closest.position);
            SetState(UnitState.Chasing);
        }
        else
        {
            SetTargetPosition(GetNearestWaypoint());
            SetState(UnitState.Wandering);
        }
        }

    void Update()
    {
        switch (currentState)
        {
            case UnitState.Wandering: break;
            case UnitState.Chasing: break;
            case UnitState.Avoiding: break;
        }
    }

    private void SetTargetPosition(Vector3 pos)
    {
        targetPos = pos;
    }

    private void FollowNextWaypoint()
    /*Set our destination to the next waypoint*/
    {
        if (waypoints.Length == 0) return;

        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        SetTargetPosition(waypoints[currentWaypoint]);
    }

    private Vector3 GetNearestWaypoint()
    {
        if (waypoints.Length == 0)
        {
            // no waypoints --> pick random position biased to cover new ground
            // TODO
            return new Vector3(297, 29, 717); // temporary value
        }

        return waypoints.Aggregate((a, b) => // return closest
            Vector3.Distance(transform.position, a) < Vector3.Distance(transform.position, b) ? a : b);
    }

    Transform GetClosest(Collider[] hits)
    {
        Transform closest = null;
        float minDist = Mathf.Infinity;
        foreach (Collider c in hits)
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < minDist) { minDist = d; closest = c.transform; }
        }
        return closest;
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    private void SetState(UnitState newState) { currentState = newState; }
}
