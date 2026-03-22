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
    public float repathInterval = 0.8f;
    public float wanderingSpeed = 5f; // for when no opponents are detected
    public float chasingSpeed = 10f;
    public float detectionRange = 200f;
    public float playerBias = 0.5f; // -1 avoid player, 0 neutral, 0.5 tend to pick player more, 1 pick player always

    private NavMeshAgent agent;
    private MovementController movementController;
    private UnitState currentState;
    private int currentWaypoint = 0;
    private int pathIndex = 0;
    private NavMeshPath currentPath;
    private float repathTimer = 0f;
    private Vector3 targetPos; // this unit will move towards this point
    private int unitTargetId; // the ID of the unit we want this unit to attack
    private float currentSpeed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<MovementController>();

        currentPath = new NavMeshPath();

        agent.updatePosition = false;  // movement system owns position
        agent.updateRotation = false;  // optional, if your system handles turning

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
        repathTimer += Time.deltaTime;
        agent.nextPosition = transform.position;
        
        if (repathTimer >= repathInterval)
        {
            repathTimer = 0f;
            RecalculatePath(targetPos);
        }

        if (currentPath.corners.Length > 0)
        {
            if (pathIndex < currentPath.corners.Length - 1)
            {
                Vector3 current = currentPath.corners[pathIndex];
                Vector3 next = currentPath.corners[pathIndex + 1];

                float distToCurrent = Vector3.Distance(transform.position, current);
                float distToNext = Vector3.Distance(transform.position, next);

                // Closer to next corner than current, and have overshot by at least 0.5f
                if (distToNext < distToCurrent && distToCurrent > 0.5f)
                    pathIndex++;

                Vector3 toCorner = currentPath.corners[pathIndex] - transform.position;
                toCorner.y = 0f;
                Vector3 localDir = transform.InverseTransformDirection(toCorner.normalized);
                movementController.moveInput = new Vector2(localDir.x, localDir.z);
            }
        }
        else
            movementController.moveInput = Vector2.zero;

        switch (currentState)
        {
            case UnitState.Wandering: 
                agent.speed = wanderingSpeed;
                if (currentPath.status == NavMeshPathStatus.PathComplete &&
                    pathIndex == currentPath.corners.Length - 1 &&
                    Vector3.Distance(transform.position, targetPos) < 1f)
                    FollowNextWaypoint();
                break;
            case UnitState.Chasing:
                agent.speed = chasingSpeed;
                break;
            case UnitState.Avoiding: break;
        }

        Vector3 desiredVelocity = agent.desiredVelocity;
    }

    private void SetTargetPosition(Vector3 pos)
    {
        targetPos = pos;
        RecalculatePath(pos);
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

    public void RecalculatePath(Vector3 destination)
    {
        agent.CalculatePath(destination, currentPath);
        pathIndex = 0;
    }

    void OnCollisionEnter(Collision col) // TODO: might reset on every bullet hit?
    {
        RecalculatePath(targetPos);
        repathTimer = 0f;
    }

    // Visualize detection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    private void SetState(UnitState newState) { currentState = newState; }
}
