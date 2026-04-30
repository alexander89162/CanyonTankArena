using UnityEngine;
using UnityEngine.AI;

/*Chases with prediction by approaching the nearest side of the target
relative to itself. Fully controls movement and partially animation.*/
public class PredictionChase : MonoBehaviour
{
    public Transform enemyTarget;
    public float repathInterval = 0.3f;
    public float standardMoveStep = 40f;
    public float minRepathInterval = 0.1f;
    public float maxRepathInterval = 3f;
    public float idealCircleRadius = 130f; // AI will try to circle at this distance
    public float sideStepGrowthRate = 0.5f; // how fast moveDestination moves away (sideways) from target as distance to target increases
    public float collisionAvoidanceMultiplier = 2f;
    public float sampleFallbackRadius = 100f;
    public float maxEngagementDist = 500f;
    public int unstuckProbes = 8;
    public float unstuckProbeDist = 60f;

    private NavMeshAgent agent;
    private TankSlopeForRig tankSlope;
    private AttackState currentState;
    private float repathTimer = 0f;
    private float moveStep = 40f;
    private Vector3 moveDestination;
    private float orbitSign = 1;
    private Vector3 toEnemy;
    private Vector3 sidePoint;
    private Vector3 desiredDir;
    private Vector3 lastPos; // to compute velocity
    private Vector3 lastForward; // to compute angular velocity
    private NavMeshHit hit;
    private float dist = 0f;
    private Vector3 exitPoint {get; set;}

    [Space(12)]
    public bool debug = false;

    public enum AttackState
    {
        Deploying,
        Wandering,
        CatchingUp,
        Circling,
        Unstucking // high repathInterval, commit to a specific path to avoid being stuck
    }

    void Awake()
    {
        tankSlope = GetComponent<TankSlopeForRig>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateUpAxis = false;
        agent.autoBraking = false;
        moveDestination = transform.position;
        enemyTarget = GameObject.FindWithTag("Player").transform.root;

        lastPos = transform.position;
        lastForward = transform.forward;
        idealCircleRadius += Random.Range(-30f, 30f);

        SetState(AttackState.Deploying);
    }

    void Start()
    {
        var holder = GetComponent<UnitDataHolder>();
        if (holder != null)
            exitPoint = holder.data.spawnPoint.exitPoint;
        else
            SetState(AttackState.CatchingUp); // this unit was manually placed (not spawned by spawn system), so skip deployment logic

        #if UNITY_EDITOR
        if (debug) Debug.Log($"PredictionChase on {gameObject.name} exitPoint:{exitPoint}");
        #endif
    }

    void Update()
    {
        Vector3 vel = (transform.position - lastPos) / Time.deltaTime;
        tankSlope.UpdateAlignment(vel);
        lastPos = transform.position;

        repathTimer += Time.deltaTime;

        if (currentState == AttackState.Deploying
            || currentState == AttackState.CatchingUp 
            || currentState == AttackState.Circling
            || currentState == AttackState.Unstucking)
        {
            if (repathTimer > repathInterval)
            {
                float angularVel = Vector3.Angle(transform.forward, lastForward) / Time.deltaTime;
                lastForward = transform.forward;

                if (vel.magnitude < 0.3f && angularVel < 3f)
                {
                    SetState(AttackState.Unstucking);
                    repathInterval = 2.5f;
                }
                else
                    AdjustRepathInterval(vel);

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
            case AttackState.Deploying:

                if (NavMesh.SamplePosition(exitPoint, out hit, sampleFallbackRadius, NavMesh.AllAreas))
                    moveDestination = hit.position;
                agent.SetDestination(moveDestination);

                Vector2 flatPos = new Vector2(transform.position.x, transform.position.z);
                Vector2 flatExit = new Vector2(exitPoint.x, exitPoint.z);
                if ((flatPos - flatExit).sqrMagnitude < 6400) // 80*80
                    SetState(AttackState.CatchingUp);
                return;
            case AttackState.Wandering: return;
            case AttackState.CatchingUp:
                toEnemy = enemyTarget.position - transform.position;
                dist = toEnemy.magnitude;
                Vector3 toSelf = -toEnemy;

                float sideDot = Vector3.Dot(toSelf.normalized, enemyTarget.right);
                orbitSign = sideDot >= 0f ? 1f : -1f;
                sidePoint = enemyTarget.position + enemyTarget.right * orbitSign * idealCircleRadius;

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
                return;
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
                float urgency = Mathf.Clamp01(Vector3.Dot(transform.forward, toEnemy.normalized)) * collisionAvoidanceMultiplier;
                float bestOrbitSign = Vector3.Dot(transform.forward, right) >= 0f ? 1f : -1f;
                Vector3 tangent = right * bestOrbitSign;
                if (dist > idealCircleRadius) urgency *= 0.5f;
                desiredDir = Vector3.Lerp(desiredDir, tangent, urgency).normalized;
                
                moveDestination = transform.position + desiredDir * moveStep;

                if (NavMesh.SamplePosition(moveDestination, out hit, sampleFallbackRadius, NavMesh.AllAreas))
                    moveDestination = hit.position;

                agent.SetDestination(moveDestination);

                if (dist > idealCircleRadius * 1.2f)
                {
                    SetState(AttackState.CatchingUp);
                    return;
                }
                //if (debug) Debug.Log($"dist:{dist} sideOffset:{sideOffset} orbitSign:{orbitSign} sidePoint:{sidePoint} desiredDir:{desiredDir}");

                return;
            case AttackState.Unstucking:
                float bestScore = float.MaxValue;
                Vector3 bestProbe = transform.position;

                for (int i = 0; i < unstuckProbes; i++)
                {
                    float angle = i * (360f / unstuckProbes) * Mathf.Deg2Rad;
                    Vector3 candidate = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * unstuckProbeDist;

                    if (NavMesh.SamplePosition(candidate, out hit, sampleFallbackRadius, NavMesh.AllAreas))
                    {
                        float sqrDistToPlayer = (hit.position - enemyTarget.position).sqrMagnitude;
                        float idealSqr = idealCircleRadius * idealCircleRadius;
                        float score = Mathf.Abs(sqrDistToPlayer - idealSqr);

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestProbe = hit.position;
                        }
                    }
                }

                moveDestination = bestProbe;
                agent.SetDestination(moveDestination);
                
                if ((transform.position - lastPos).magnitude / Time.deltaTime > 0.5f)
                {
                    SetState(AttackState.CatchingUp);
                    repathTimer = 0f;
                }

                return;
        }
    }

    private void AdjustRepathInterval(Vector3 vel)
    {
        // Set repathInterval based on distance from target
        float dist = (enemyTarget.position - transform.position).magnitude;
        float t = Mathf.Clamp01(dist / maxEngagementDist);
        repathInterval = Mathf.Lerp(minRepathInterval, maxRepathInterval, t);
        moveStep = Mathf.Clamp(standardMoveStep * t, 20f, 990f); // closer = smaller steps, farther = bigger steps
    }

    public void SetState(AttackState newState)
    {
        #if UNITY_EDITOR
        if (debug) Debug.Log($"Changed from {currentState} to {newState}");
        #endif
        currentState = newState;
    }
}
