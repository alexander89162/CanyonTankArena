using UnityEngine;
using UnityEngine.AI;

/*This script handles the AI and animation logic all in one.
AI: 3 stages; move near player, rising into air/launching self, and recharge
Animation: basic roll on the ground, spin slowly when lifting and fast when launched*/
public class SpikeballController : MonoBehaviour
{
    public Transform enemyTarget;
    public Transform spikeballRenderer;
    public float movementSpeed = 20f;
    public float repathInterval = 0.8f;
    public float moveStep = 40f;
    public float minChargeDistance = 40f;
    public float liftingTime = 4f;
    public float liftingHeight = 30f;
    public float launchRechargeTime = 3f;
    public float launchStrengthMultiplier = 1f;
    public float gravityMultiplier = 1f;
    public float exitFreeRoamVel = 3f; // when velocity drops this amount, exit FreeRoam state and return to NavMesh
    public float minFreeRoamTime = 0.5f;
    public float radius = 9f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private AttackState currentState;
    private Vector3 moveDestination;
    private Vector3 liftingStartPos;
    private Vector3 liftingEndPos;
    private float repathTimer = 0f;
    private float liftingTimer = 0f;
    private float freeRoamTimer = 0f;
    private Vector3 lastPos;

    public bool debug = false;

    public enum AttackState
    {
        Approaching, // roll towards target with NavMesh
        Lifting, // Lerp position
        FreeRoam // free roam with physics using rigidbody until most momentum is lost (velocity < exitFreeRoamVel). This state handles both launching and bouncing
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // rb only used for Launching state
        rb.useGravity = true;
        moveDestination = transform.position;

        SetState(AttackState.Approaching);
    }

    void Update()
    {
        repathTimer += Time.deltaTime;

        /* Manually handle all time-sensitive states first, if none of 
        these then go to general Repath()*/
        if (currentState == AttackState.Lifting)
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
                float currentVelocity = Vector3.Distance(transform.position, lastPos) / Time.deltaTime;
                if (currentVelocity < exitFreeRoamVel)
                {
                    if (debug) Debug.Log($"Exited {currentState} because the spikeball was moving below {exitFreeRoamVel} m/s.");
                    SetState(AttackState.Approaching);
                }
            }
        }
        else if (repathTimer > repathInterval) // general Repath case
        {
            Repath();
        }

        lastPos = transform.position;
        AnimateSelf();
    }

    private void Repath()
    {
        repathTimer = 0f;
        if (debug) Debug.Log($"Repath() called when in the {currentState} state");
        
        switch (currentState) // some states are handled in Update() instead of here since they're more time-sensitive
        {
            case AttackState.Approaching:
                Vector3 toEnemy = enemyTarget.position - transform.position;
                Vector3 toSelf = -toEnemy;

                float sideDot = Vector3.Dot(toSelf.normalized, enemyTarget.right);
                float orbitSign = sideDot >= 0f ? 1f : -1f;
                Vector3 sidePoint = enemyTarget.position + enemyTarget.right * (orbitSign * minChargeDistance);

                if (Vector3.Distance(transform.position, sidePoint) < minChargeDistance)
                {
                    liftingStartPos = transform.position;
                    liftingEndPos = liftingStartPos + new Vector3(0, liftingHeight, 0);
                    SetState(AttackState.Lifting);
                    return;
                }
                Vector3 desiredDir = (sidePoint - transform.position).normalized;
                Vector3 desiredTarget = transform.position + (desiredDir * moveStep);
                moveDestination = desiredTarget;
                break;
        }
        agent.SetDestination(moveDestination);
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
            case AttackState.Lifting: break;
            case AttackState.FreeRoam: break;
        }
    }

    ///<summary>Set the current state and handle all state changes for the specified transition</summary>
    public void SetState(AttackState newState)
    {
        switch (newState)
        {
            case AttackState.Approaching:
                agent.enabled = true;
                rb.isKinematic = true;
                repathTimer = 0f;
                break;
            case AttackState.Lifting:
                agent.enabled = false;
                liftingTimer = 0f;
                break;
            case AttackState.FreeRoam:
                // Deactivate NavMesh agent and prepare RigidBody
                agent.enabled = false;
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                freeRoamTimer = 0f;

                // Find direction to player, aim and launch
                Vector3 toTarget = (enemyTarget.position - transform.position).normalized;
                Vector3 launchDir = new Vector3(toTarget.x, toTarget.y + 1.2f, toTarget.z).normalized;
                rb.AddForce(launchDir * rb.mass * launchStrengthMultiplier, ForceMode.Impulse);

                break;
        }

        if (debug) Debug.Log($"Changed from {currentState} to {newState}");
        currentState = newState;
    }
}
