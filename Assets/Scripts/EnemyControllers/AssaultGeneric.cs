using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/*Circle an opponent once spotting them. Using probabilities, the unit explores 
the map in Wandering mode (slower movement and shorter vision range) until finding 
an opponent. There is support for multiple units on enemy and ally teams*/
public class AssaultGeneric : MonoBehaviour
{
    private enum UnitState
    {
        Wandering,
        Chasing,
        Avoiding
    }

    public LayerMask opponentLayer;
    public GameObject[] opponents;
    public float repathInterval = 0.8f;
    public float wanderingSpeed = 7f; // for when no opponents are detected
    public float chasingSpeed = 15f;
    public float detectionRange = 80f;
    public float playerBias = 0.5f; // -1 avoid player, 0 neutral, 0.5 tend to pick player more, 1 pick player always
    public float idealCircleRadius = 50f; // AI will try to circle at this distance
    public float moveStep = 20f;
    public float angularSpeed = 10f;

    private NavMeshAgent agent;
    private UnitState currentState;
    private float repathTimer = 0f;
    public Transform enemyTarget; // enemy to attack
    public Vector3 moveDestination; // position to go towards
    private TankSlopeForRig tankSlope;
    private Vector3 lastPosition;
    private Vector3 currentForward;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        tankSlope = GetComponent<TankSlopeForRig>();

        lastPosition = transform.position;
        currentForward = transform.forward;
        agent.updateUpAxis = false;

        SetState(UnitState.Chasing);
    }

    void Update()
    {
        Vector3 vel = (transform.position - lastPosition) / Time.deltaTime;
        tankSlope.UpdateAlignment(vel);

        lastPosition = transform.position;
        repathTimer += Time.deltaTime;
        transform.position = agent.nextPosition;
        
        if (repathTimer >= repathInterval)
        {
            Repath();
            repathTimer = 0f;
        }
    }

    ///<summary>Recompute the enemyTarget, moveDestination, and agent's path. 
    /// The current state effects the results.</summary>
    void Repath()
    {
        currentForward = agent.velocity.normalized; // lazy update of state variables

        switch (currentState)
        {
            case UnitState.Wandering: break;
            case UnitState.Chasing:
                // 1) Pick direction the AI will move towards
                Vector3 toTarget = enemyTarget.position - transform.position;

                // Treat relative direction differences as a Vector2
                Vector3 flatForward = new Vector3(toTarget.x, toTarget.y, 0f);
                flatForward.Normalize();
                Vector3 side = new Vector3(-flatForward.y, flatForward.x, 0f); // Perpendicular
                
                float sideDot = Vector3.Dot(transform.right, toTarget);
                float currentSideSign = (sideDot > 0f) ? 1f : -1f; // Decide direction
                Vector3 desiredDir = (flatForward + side * currentSideSign).normalized;
                
                // 2) Build our first guess at next target position
                Vector3 desiredTarget = transform.position + (desiredDir * moveStep);

                moveDestination = desiredTarget;
                break;
            case UnitState.Avoiding: break;
        }

        agent.SetDestination(moveDestination);
    }

    ///<summary>Set the unit's current state to alter behavior. This method also 
    /// handles initialization logic for the new state</summary>
    private void SetState(UnitState newState)
    {
        currentState = newState;
    }
}
