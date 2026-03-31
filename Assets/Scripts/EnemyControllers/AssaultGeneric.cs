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
    public float wanderingSpeed = 5f; // for when no opponents are detected
    public float chasingSpeed = 10f;
    public float detectionRange = 80f;
    public float playerBias = 0.5f; // -1 avoid player, 0 neutral, 0.5 tend to pick player more, 1 pick player always

    private NavMeshAgent agent;
    private UnitState currentState;
    private float repathTimer = 0f;
    public Transform enemyTarget; // enemy to attack
    public Vector3 moveDestination; // position to go towards
    private TankSlopeForRig tankSlope;
    private Vector3 lastPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        tankSlope = GetComponent<TankSlopeForRig>();

        lastPosition = transform.position;
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
        switch (currentState)
        {
            case UnitState.Wandering: break;
            case UnitState.Chasing:
                moveDestination = enemyTarget.position;
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
