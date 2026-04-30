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

    [Header("Movement Audio")]
    [SerializeField] private AudioClip movementSound;
    [SerializeField, Range(0f, 1f)] private float movementSoundVolume = 0.9f;
    [SerializeField] private float movementSoundFadeOutTime = 1f;

    [Header("Turn Audio")]
    [SerializeField] private AudioClip turnSound;
    [SerializeField, Range(0f, 1f)] private float turnSoundVolume = 0.85f;
    [SerializeField] private float turnSoundFadeOutTime = 0.15f;

    public Vector2 moveInput = Vector2.zero; 
    public bool isBoosting = false;

    private CharacterController controller;
    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity = 0f;
    public Vector3 currentVelocityV = Vector3.zero;
    private AudioSource movementAudioSource;
    private AudioSource turnAudioSource;

    private TankSlopeForRig tankSlopeRig;
    private TankSlope tankSlope;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        tankSlopeRig = GetComponent<TankSlopeForRig>();
        tankSlope = GetComponent<TankSlope>();

        EnsureMovementAudioSource();
        EnsureTurnAudioSource();
        TryAssignMovementSound();
        TryAssignTurnSound();

        if (tankSlopeRig != null)
            tankSlopeRig.tankRoot = transform;
        else if (tankSlope != null)
            tankSlope.tankRoot = transform;
    }

    private void Reset()
    {
        TryAssignMovementSound();
        TryAssignTurnSound();
    }

    private void OnValidate()
    {
        TryAssignMovementSound();
        TryAssignTurnSound();
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

        UpdateMovementAudio();
        UpdateTurnAudio();
    }

    private void UpdateMovementAudio()
    {
        if (movementAudioSource == null || movementSound == null)
            return;

        if (Time.timeScale <= 0f)
        {
            StopAudioSource(movementAudioSource);
            return;
        }

        float moveStrength = Mathf.Clamp01(Mathf.Abs(moveInput.x) + Mathf.Abs(moveInput.y));

        if (moveStrength > 0.01f)
        {
            if (!movementAudioSource.isPlaying)
            {
                movementAudioSource.clip = movementSound;
                movementAudioSource.loop = true;
                movementAudioSource.Play();
            }

            movementAudioSource.volume = movementSoundVolume * moveStrength;
            return;
        }

        if (!movementAudioSource.isPlaying)
            return;

        float fadeOutSpeed = 1f / Mathf.Max(0.01f, movementSoundFadeOutTime);
        movementAudioSource.volume = Mathf.MoveTowards(movementAudioSource.volume, 0f, fadeOutSpeed * Time.deltaTime);

        if (movementAudioSource.volume <= 0.001f)
        {
            movementAudioSource.Stop();
            movementAudioSource.time = 0f;
        }
    }

    private void UpdateTurnAudio()
    {
        if (turnAudioSource == null || turnSound == null)
            return;

        if (Time.timeScale <= 0f)
        {
            StopAudioSource(turnAudioSource);
            return;
        }

        float turnStrength = Mathf.Abs(moveInput.x);

        if (turnStrength > 0.01f)
        {
            if (!turnAudioSource.isPlaying)
            {
                turnAudioSource.clip = turnSound;
                turnAudioSource.loop = true;
                turnAudioSource.Play();
            }

            turnAudioSource.volume = turnSoundVolume * turnStrength;
            return;
        }

        if (!turnAudioSource.isPlaying)
            return;

        float fadeOutSpeed = 1f / Mathf.Max(0.01f, turnSoundFadeOutTime);
        turnAudioSource.volume = Mathf.MoveTowards(turnAudioSource.volume, 0f, fadeOutSpeed * Time.deltaTime);

        if (turnAudioSource.volume <= 0.001f)
        {
            StopAudioSource(turnAudioSource);
        }
    }

    private static void StopAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.volume = 0f;
    }

    private void EnsureMovementAudioSource()
    {
        if (movementAudioSource != null)
            return;

        Transform audioRoot = transform.Find("MovementAudio");
        if (audioRoot == null)
        {
            GameObject audioObject = new GameObject("MovementAudio");
            audioObject.transform.SetParent(transform, false);
            audioRoot = audioObject.transform;
        }

        movementAudioSource = audioRoot.GetComponent<AudioSource>();
        if (movementAudioSource == null)
            movementAudioSource = audioRoot.gameObject.AddComponent<AudioSource>();

        movementAudioSource.playOnAwake = false;
        movementAudioSource.loop = true;
        movementAudioSource.spatialBlend = 0f;
        movementAudioSource.dopplerLevel = 0f;
    }

    private void EnsureTurnAudioSource()
    {
        if (turnAudioSource != null)
            return;

        Transform audioRoot = transform.Find("TurnAudio");
        if (audioRoot == null)
        {
            GameObject audioObject = new GameObject("TurnAudio");
            audioObject.transform.SetParent(transform, false);
            audioRoot = audioObject.transform;
        }

        turnAudioSource = audioRoot.GetComponent<AudioSource>();
        if (turnAudioSource == null)
            turnAudioSource = audioRoot.gameObject.AddComponent<AudioSource>();

        turnAudioSource.playOnAwake = false;
        turnAudioSource.loop = true;
        turnAudioSource.spatialBlend = 0f;
        turnAudioSource.dopplerLevel = 0f;
    }

    private void TryAssignMovementSound()
    {
#if UNITY_EDITOR
        if (movementSound == null)
            movementSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/tankmovement.mp3");
#endif
    }

    private void TryAssignTurnSound()
    {
#if UNITY_EDITOR
        if (turnSound == null)
            turnSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/turretturning.mp3");
#endif
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