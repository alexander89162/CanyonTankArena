using UnityEngine;
using UnityEngine.AI;

/*This script handles the AI and animation logic all in one.
AI: 3 stages; move near player, rising into air/launching self, and recharge
Animation: basic roll on the ground, spin slowly when lifting and fast when launched*/
public class SpikeballController : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;

    public float movementSpeed = 20f;
    public float repathInterval = 0.8f;
    public float liftingTime = 2f;
    public float liftingHeight = 30f;
    public float launchRechargeTime = 3f;
    public float radius = 9f;

    private AttackState currentState;
    private float repathTimer = 0f;
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

        SetState(AttackState.Approaching);
    }

    void Update()
    {
        repathTimer += Time.deltaTime;


        if (repathTimer > repathInterval)
        {
            Repath();
            repathTimer = 0f;
        }

        AnimateSelf();
    }

    private void Repath()
    {
        switch (currentState)
        {
            case AttackState.Approaching: break;
            case AttackState.Lifting: break;
            case AttackState.Launching: break;
        }
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
