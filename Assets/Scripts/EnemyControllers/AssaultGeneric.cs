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
    public Vector3[] waypoints; // defaults for target when no unit target is seen
    public float repathInterval = 0.8f;
    public float wanderingSpeed = 5f; // for when no opponents are detected
    public float chasingSpeed = 10f;
    public float detectionRange = 200f;
    public float playerBias = 0.5f; // -1 avoid player, 0 neutral, 0.5 tend to pick player more, 1 pick player always

    private NavMeshAgent agent;
    private UnitState currentState;
    private float repathTimer = 0f;
    public Transform target;
    private float currentSpeed;
    private TankSlope tankSlope;
    private Vector3 lastPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        tankSlope = GetComponent<TankSlope>();

        lastPosition = transform.position;
        agent.updateUpAxis = false;

        SetState(UnitState.Chasing);
    }

    void Update()
    {
        agent.SetDestination(target.position);

        Vector3 vel = (transform.position - lastPosition) / Time.deltaTime;
        tankSlope.UpdateAlignment(vel);
        lastPosition = transform.position;

        repathTimer += Time.deltaTime;
        transform.position = agent.nextPosition;
        
        if (repathTimer >= repathInterval)
        {
            repathTimer = 0f;
        }

        switch (currentState)
        {
            case UnitState.Wandering: break;
            case UnitState.Chasing: break;
            case UnitState.Avoiding: break;
        }
    }

    void OnCollisionEnter(Collision col) // TODO: might reset on every bullet hit?
    {
        repathTimer = 0f;
    }

    private void SetState(UnitState newState) { currentState = newState; }
}
