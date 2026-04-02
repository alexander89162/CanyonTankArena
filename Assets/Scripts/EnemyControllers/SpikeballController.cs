using UnityEngine;
using UnityEngine.AI;

/*This script handles the AI and animation logic all in one.
AI: 3 stages; move near player, rising into air/launching self, and recharge
Animation: basic roll on the ground, spin slowly when lifting and fast when launched*/
public class SpikeballController : MonoBehaviour
{
    public Transform enemyTarget;
    public float movementSpeed = 20f;
    public float repathInterval = 0.8f;
    public float moveStep = 40f;
    public float minChargeDistance = 40f;
    public float liftingTime = 4f;
    public float liftingHeight = 30f;
    public float launchRechargeTime = 3f;
    public float radius = 9f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private AttackState currentState;
    private Vector3 moveDestination;
    private Vector3 liftingStartPos;
    private Vector3 liftingEndPos;
    private float repathTimer = 0f;
    private float liftingTimer = 0f;
    private Vector3 lastPos;

    public bool debug = false;

    public enum AttackState
    {
        Approaching,
        Lifting,
        Launching
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // rb only used for Launching state
        moveDestination = transform.position;

        SetState(AttackState.Approaching);
    }

    void Update()
    {
        repathTimer += Time.deltaTime;

        // manually handle all time-sensitive states, if none of these then go to general Repath()
        if (currentState == AttackState.Lifting)
        {
            liftingTimer += Time.deltaTime;
            float t = liftingTimer / liftingTime;
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(liftingStartPos, liftingEndPos, t);

            if (t >= 1)
            {
                SetState(AttackState.Launching);
                liftingTimer = 0f;
            }
        }
        else if (currentState == AttackState.Launching)
        {
            //
        }
        else if (repathTimer > repathInterval)
        {
            Repath();
            repathTimer = 0f;
        }

        AnimateSelf();
    }

    private void Repath()
    {
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
                    break;
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
                transform.Rotate(Vector3.Cross(moveDir, Vector3.up), -rotationAngle, Space.World);
                break;
            case AttackState.Lifting: break;
            case AttackState.Launching: break;
        }
        lastPos = transform.position;
    }

    public void SetState(AttackState newState)
    {
        if (debug) Debug.Log($"Changed from {currentState} to {newState}");
        currentState = newState;
    }
}
