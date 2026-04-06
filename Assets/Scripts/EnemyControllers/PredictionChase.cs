using UnityEngine;
using UnityEngine.AI;

/*Chases with prediction by approaching the nearest side of the target
relative to itself. Fully controls movement. Animation is handled automatically
by external components*/
public class PredictionChase : MonoBehaviour
{
    public Transform enemyTarget;
    public float repathInterval = 0.3f;
    public float moveStep = 40f;
    public float idealCircleRadius = 50f; // AI will try to circle at this distance
    public float sideStepGrowthRate = 0.5f; // how fast moveDestination moves away (sideways) from target as distance to target increases

    private NavMeshAgent agent;
    private AttackState currentState;
    private float repathTimer = 0f;
    private Vector3 moveDestination;
    private float orbitSign = 1;
    private Vector3 toEnemy;
    private Vector3 sidePoint;
    private Vector3 desiredDir;
    private NavMeshHit hit;
    private float dist = 0f;

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
        agent.autoBraking = false;
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
            case AttackState.CatchingUp:
                toEnemy = enemyTarget.position - transform.position;
                dist = toEnemy.magnitude;
                Vector3 toSelf = -toEnemy;

                float sideDot = Vector3.Dot(toSelf.normalized, enemyTarget.right);
                orbitSign = sideDot >= 0f ? 1f : -1f;
                sidePoint = enemyTarget.position + enemyTarget.right * (orbitSign * idealCircleRadius);

                desiredDir = (sidePoint - transform.position).normalized;
                Vector3 desiredTarget = transform.position + (desiredDir * moveStep);
                if (NavMesh.SamplePosition(desiredTarget, out hit, moveStep * 3, NavMesh.AllAreas))
                    moveDestination = hit.position;

                agent.SetDestination(moveDestination);

                if (dist < idealCircleRadius)
                {
                    SetState(AttackState.Circling);
                    return;
                }
                break;
            case AttackState.Circling:
                toEnemy = enemyTarget.position - transform.position;
                dist = toEnemy.magnitude;

                Vector3 right = Vector3.Cross(Vector3.up, toEnemy.normalized).normalized;
                if (dist > idealCircleRadius)
                {
                    orbitSign = Vector3.Dot(transform.forward, right) >= 0f ? 1f : -1f;
                }
                float sideOffset = idealCircleRadius + Mathf.Max(0f, dist - idealCircleRadius) * sideStepGrowthRate;
                sidePoint = enemyTarget.position + right * (orbitSign * sideOffset);

                desiredDir = (sidePoint - transform.position).normalized;

                // turn away from collision course if moving into enemy
                float urgency = Mathf.Clamp01(Vector3.Dot(transform.forward, toEnemy.normalized)) * 1.5f;
                float bestOrbitSign = Vector3.Dot(transform.forward, right) >= 0f ? 1f : -1f;
                Vector3 tangent = right * (urgency > 0.7f ? bestOrbitSign : orbitSign);
                if (dist > idealCircleRadius) urgency = 0;
                desiredDir = Vector3.Lerp(desiredDir, tangent, urgency).normalized;
                
                moveDestination = transform.position + desiredDir * moveStep;

                if (NavMesh.SamplePosition(moveDestination, out hit, moveStep, NavMesh.AllAreas))
                    moveDestination = hit.position;
                if (debug) Debug.Log($"SamplePosition hit.position: {hit.position}");

                agent.SetDestination(moveDestination);

                if (dist > idealCircleRadius * 1.2f)
                {
                    SetState(AttackState.CatchingUp);
                    return;
                }
                if (debug) Debug.Log($"dist:{dist} sideOffset:{sideOffset} orbitSign:{orbitSign} sidePoint:{sidePoint} desiredDir:{desiredDir}");

                break;
        }
    }

    public void SetState(AttackState newState)
    {
        // switch (newState)
        // {
        //     case AttackState.Deploying: break;
        //     case AttackState.Wandering: break;
        //     case AttackState.CatchingUp: break;
        //     case AttackState.Circling: break;
        // }

        #if UNITY_EDITOR
        if (debug) Debug.Log($"Changed from {currentState} to {newState}");
        #endif
        currentState = newState;
    }
}
