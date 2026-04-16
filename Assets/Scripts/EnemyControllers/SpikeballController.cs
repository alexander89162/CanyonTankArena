using UnityEngine;
using UnityEngine.AI;

/*This script handles the AI and animation logic all in one.
AI: 3 stages; move near player, rising into air/launching self, and recharge.
Animation: basic roll on the ground, spin increasingly fast when lifting and 
slowly when launched*/
public class SpikeballController : MonoBehaviour
{
    public Transform enemyTarget;
    public Transform spikeballRenderer;
    public float repathInterval = 0.8f;
    public float moveStep = 40f;
    public float minChargeDistance = 80f;
    public float liftingTime = 4f;
    public float liftingHeight = 30f;
    public float liftSpinSpeed = 150f;
    public float launchSpinSpeed = 500f;
    public float launchRechargeTime = 5.5f;
    public float launchStrengthMultiplier = 370f;
    public float returnForceMultiplier = 15f;
    public float launchArcBias = 0.05f;
    public float bounceMomentumLoss = 0.6f; // 1 = no loss, 0 = full stop
    public float gravityMultiplier = 6f;
    public float groundedCooloffTime = 0.1f;
    public float airControlMultiplier = 0.4f;
    public float exitFreeRoamVel = 12f; // when velocity drops this amount, exit FreeRoam state and return to NavMesh
    public float minFreeRoamTime = 6f;
    public float radius = 9f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    public AttackState currentState;
    private Vector3 moveDestination;
    private Vector3 liftingStartPos;
    private Vector3 liftingEndPos;
    private Vector3 launchDir;
    private Vector3 rightOfLaunchDir;
    private float repathTimer = 0f;
    private float liftingTimer = 0f;
    private float freeRoamTimer = 0f;
    private float launchRechargeTimer = 0f;
    public bool hasBouncedThisLaunch = false;
    private float lastGroundedTimer = 0f;
    private Vector3 lastPos;
    [Space(12)]
    public bool debug = false;

    public enum AttackState
    {
        Approaching, // roll towards target with NavMesh
        Lifting, // Lerp position
        FreeRoam // free roam with rigidbody until most momentum is lost (velocity < exitFreeRoamVel). This state handles both launching and bouncing
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        moveDestination = transform.position;

        enemyTarget = GameObject.FindWithTag("Player").transform.root;

        SetState(AttackState.Approaching);
    }

    void Update()
    {
        repathTimer += Time.deltaTime;
        launchRechargeTimer += Time.deltaTime;

        if (currentState == AttackState.Approaching)
        {
            float sqrDist = (enemyTarget.position - transform.position).sqrMagnitude;
            if (launchRechargeTimer > launchRechargeTime && sqrDist < minChargeDistance * minChargeDistance)
                SetState(AttackState.Lifting);
            else if (repathTimer > repathInterval)
                Repath();
        }
        else if (currentState == AttackState.Lifting)
        {
            liftingTimer += Time.deltaTime;
            float t = liftingTimer / liftingTime;
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(liftingStartPos, liftingEndPos, t);

            if (t >= 1)
            {
                SetState(AttackState.FreeRoam);
            }
        }
        else if (currentState == AttackState.FreeRoam)
        {
            freeRoamTimer += Time.deltaTime;

            if (freeRoamTimer >= minFreeRoamTime)
            {
                if (rb.linearVelocity.sqrMagnitude < exitFreeRoamVel * exitFreeRoamVel)
                {
                    #if UNITY_EDITOR
                    if (debug) Debug.Log($"Exited {currentState} because the spikeball was moving below {exitFreeRoamVel} m/s.");
                    #endif
                    SetState(AttackState.Approaching);
                }
            }
        }

        AnimateSelf();
        lastPos = transform.position;
    }

    void FixedUpdate()
    {
        lastGroundedTimer -= Time.deltaTime;

        if (currentState == AttackState.FreeRoam)
        {
            if (lastGroundedTimer < -10f)
            {
                if ((transform.position - enemyTarget.position).sqrMagnitude > 40000f)
                {
                    // destroy spikeball; probably went off the arena
                    // TODO: signal death to UnitManager
                    Destroy(gameObject);
                }
            }

            if (hasBouncedThisLaunch && lastGroundedTimer < 0f) rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

            if ((enemyTarget.position - transform.position).sqrMagnitude > minChargeDistance * minChargeDistance * 4)
            {
                Vector3 toTarget = (enemyTarget.position - transform.position).normalized;
                rb.AddForce(toTarget * returnForceMultiplier * 
                    (lastGroundedTimer > 0f ? 1f : airControlMultiplier), ForceMode.Acceleration);
            }
        }
    }

    private void Repath()
    {
        repathTimer = 0f;
        
        switch (currentState) // some states are handled in Update() instead of here since they're more time-sensitive
        {
            case AttackState.Approaching:
                Vector3 toEnemy = enemyTarget.position - transform.position;
                Vector3 toSelf = -toEnemy;

                float sideDot = Vector3.Dot(toSelf.normalized, enemyTarget.right);
                float orbitSign = sideDot >= 0f ? 1f : -1f;
                Vector3 sidePoint = enemyTarget.position + enemyTarget.right * (orbitSign * minChargeDistance);

                if ((transform.position - sidePoint).sqrMagnitude < minChargeDistance * minChargeDistance)
                {
                    SetState(AttackState.Lifting);
                    return;
                }
                Vector3 desiredDir = (sidePoint - transform.position).normalized;
                Vector3 desiredTarget = transform.position + (desiredDir * moveStep);
                if (NavMesh.SamplePosition(desiredTarget, out NavMeshHit hit, moveStep * 3, NavMesh.AllAreas))
                    moveDestination = hit.position;
                break;
        }
        agent.SetDestination(moveDestination);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (currentState == AttackState.FreeRoam && !hasBouncedThisLaunch)
        {
            rb.linearVelocity *= bounceMomentumLoss;
            hasBouncedThisLaunch = true; // we want only the first bounce to lose momentum
        }
    }

    void OnCollisionStay(Collision collision)
    {
        lastGroundedTimer = groundedCooloffTime;
    }

    private void AnimateSelf()
    {
        switch (currentState)
        {
            case AttackState.Approaching:
                float distanceTraveled = Vector3.Distance(transform.position, lastPos);
                float rotationAngle = distanceTraveled / radius * Mathf.Rad2Deg;
                Vector3 moveDir = (transform.position - lastPos).normalized;
                spikeballRenderer.Rotate(Vector3.Cross(moveDir, Vector3.up), -rotationAngle, Space.World);
                break;
            case AttackState.Lifting:
                float t = liftingTimer / liftingTime;

                // accelerate over time (quadratic)
                float spinSpeed = liftSpinSpeed +  2 * liftSpinSpeed * (t * t);

                // axis perpendicular to desired movement direction
                Vector3 toTarget = (enemyTarget.position - transform.position).normalized;
                Vector3 axis = Vector3.Cross(toTarget, Vector3.down);

                spikeballRenderer.Rotate(axis, spinSpeed * Time.deltaTime, Space.World);
                break;
            case AttackState.FreeRoam:
                if (!hasBouncedThisLaunch)
                {
                    // rotate along launchDir (forward) and also sideways
                    spikeballRenderer.Rotate(launchDir, launchSpinSpeed * Time.deltaTime, Space.World);
                    spikeballRenderer.Rotate(rightOfLaunchDir, launchSpinSpeed * Time.deltaTime * 0.4f, Space.World);
                }
                break;
        }
    }

    ///<summary>Set the current state and handle all state changes for the specified transition</summary>
    public void SetState(AttackState newState)
    {
        switch (newState)
        {
            case AttackState.Approaching:
                rb.isKinematic = true;
                agent.enabled = true;
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                repathTimer = 0f;
                break;
            case AttackState.Lifting:
                agent.enabled = false;
                liftingTimer = 0f;
                liftingStartPos = transform.position;
                liftingEndPos = liftingStartPos + new Vector3(0, liftingHeight, 0);
                break;
            case AttackState.FreeRoam:
                // Deactivate NavMesh agent and prepare RigidBody
                agent.enabled = false;
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                freeRoamTimer = 0f;
                launchRechargeTimer = 0f;
                hasBouncedThisLaunch = false;

                // Find direction to player, aim and launch
                Vector3 toTarget = (enemyTarget.position - transform.position).normalized;
                launchDir = new Vector3(toTarget.x, toTarget.y + launchArcBias, toTarget.z).normalized;
                rightOfLaunchDir = Vector3.Cross(launchDir, Vector3.up);
                rb.AddForce(launchDir * rb.mass * launchStrengthMultiplier, ForceMode.Impulse);
                break;
        }

        #if UNITY_EDITOR
        if (debug) Debug.Log($"Changed from {currentState} to {newState}");
        #endif
        currentState = newState;
    }
}
