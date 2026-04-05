using UnityEngine;
using UnityEngine.AI;

/*Chases with prediction by approaching the nearest side of the target
relative to itself. Fully controls movement. Animation is handled automatically
by external components*/
public class PredictionChase : MonoBehaviour
{
    public Transform enemyTarget;
    public float repathInterval = 0.1f;
    public float moveStep = 40f;
    public float idealCircleRadius = 50f; // AI will try to circle at this distance
    public float sideStepGrowthRate = 0.5f; // how fast moveDestination moves away (sideways) from target as distance to target increases
    public float steerBias = 0.9f; // how badly the unit wants to steer towards moveDestination

    private NavMeshAgent agent;
    private AttackState currentState;
    private float repathTimer = 0f;
    private Vector3 moveDestination;
    private float orbitSign = 1;

    [Space(12)]
    public bool debug = false;

    public enum AttackState
    {
        Deploying,
        Wandering,
        CatchingUp,
        Circling
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        moveDestination = transform.position;

        SetState(AttackState.Circling);
    }

    void Update()
    {
        repathTimer += Time.deltaTime;

        if (currentState == AttackState.CatchingUp 
            || currentState == AttackState.Circling)
        {
            if (repathTimer > repathInterval)
            {
                Repath();
            }
        }
    }

    ///<summary>Sets the agent's destination and resets repathTimer.</summary>
    private void Repath()
    {
        repathTimer = 0f;

        switch (currentState)
        {
            case AttackState.Deploying: break;
            case AttackState.Wandering: break;
            case AttackState.CatchingUp: break;
            case AttackState.Circling:
                Vector3 toEnemy = enemyTarget.position - transform.position;
                float dist = toEnemy.magnitude;

                Vector3 right = Vector3.Cross(Vector3.up, toEnemy.normalized).normalized;
                if (dist * dist > idealCircleRadius * idealCircleRadius)
                {
                    float sideDot = Vector3.Dot(transform.position - enemyTarget.position, right);
                    orbitSign = Vector3.Dot(transform.forward, right) >= 0f ? 1f : -1f;
                }
                float sideOffset = idealCircleRadius + Mathf.Max(0f, dist - idealCircleRadius) * sideStepGrowthRate;
                Vector3 sidePoint = enemyTarget.position + right * (orbitSign * sideOffset);

                Vector3 desiredDir = (sidePoint - transform.position).normalized;
                Vector3 biasedDir = Vector3.Lerp(transform.forward, desiredDir, steerBias).normalized;
                
                moveDestination = transform.position + biasedDir * moveStep;

                if (NavMesh.SamplePosition(moveDestination, out NavMeshHit hit, moveStep, NavMesh.AllAreas))
                    moveDestination = hit.position;

                agent.SetDestination(moveDestination);

                if (debug) Debug.Log($"dist:{dist} sideOffset:{sideOffset} orbitSign:{orbitSign} sidePoint:{sidePoint} desiredDir:{desiredDir}");

                break;
        }
    }

    public void SetState(AttackState newState)
    {
        switch (newState)
        {
            case AttackState.Deploying: break;
            case AttackState.Wandering: break;
            case AttackState.CatchingUp: break;
            case AttackState.Circling: break;
        }

        #if UNITY_EDITOR
        if (debug) Debug.Log($"Changed from {currentState} to {newState}");
        #endif
        currentState = newState;
    }
}
