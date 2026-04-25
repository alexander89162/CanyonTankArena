using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float maxSpeed;
    public float accelerationTime;
    public float decelerationTime;
    public float backwardMultiplier;
    public float boostMultiplier = 1.5f;
    public float rotationSpeed;
    public float gravity;

    public Vector2 moveInput = Vector2.zero; 
    public bool isBoosting = false;

    private CharacterController controller;
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    public Vector3 currentVelocityV = Vector3.zero;

    private TankSlopeForRig tankSlopeRig;
    private TankSlope tankSlope;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        tankSlopeRig = GetComponent<TankSlopeForRig>();
        tankSlope = GetComponent<TankSlope>();

        if (tankSlopeRig != null)
            tankSlopeRig.tankRoot = transform;
        else if (tankSlope != null)
            tankSlope.tankRoot = transform;
    }

    void Update()
    {
        HandleMovement();
    }

    public void HandleMovement()
    {
        float turnInput = moveInput.x;
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float rotationThisFrame = turnInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationThisFrame, 0);
        }

        Vector3 targetDirection = transform.forward * moveInput.y;
        float targetSpeed = Mathf.Abs(moveInput.y) * maxSpeed;

        if (isBoosting)
            targetSpeed *= boostMultiplier;

        if (moveInput.y < 0)
            targetSpeed *= backwardMultiplier;

        float smoothTime = (moveInput.y != 0) ? accelerationTime : decelerationTime;

        currentVelocity = Vector3.SmoothDamp(
            currentVelocity,
            targetDirection * targetSpeed,
            ref currentVelocityV,
            smoothTime
        );

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 move = currentVelocity;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        if (tankSlopeRig != null)
        {
            tankSlopeRig.UpdateAlignment(currentVelocity);
        }
        else if (tankSlope != null)
        {
            tankSlope.UpdateAlignment(currentVelocity); 
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Ignore ground surfaces
        if (hit.normal.y > 0.5f) return;

        // Slide sideways upon collision to make it feel less rigid
        Vector3 projected = Vector3.Dot(currentVelocity, hit.normal) * hit.normal;
        Vector3 slide = currentVelocity - projected;
        currentVelocity = Vector3.ClampMagnitude(slide * 1.5f, maxSpeed * 0.6f);
    }
}