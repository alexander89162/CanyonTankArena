using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    //Sets up look action and transform object to grab
    [SerializeField] InputActionReference lookAction;     
    [SerializeField] Transform followTarget;             
    [SerializeField] string preferredTargetName = "minitank-v10-green 1";
    [SerializeField] bool forcePreferredTarget = true;
    [SerializeField] bool enableOnlyOnPreferredTank = true;

    //syncs camera and aim controller to have better aiming
    [SerializeField] AimController aimController;

    //Variables to change how the camera feels when moving
    [SerializeField] Vector3 offset = new Vector3(2.2f, 2.2f, -8.2f);
    [SerializeField] float rotationSpeed = 90f;
    [SerializeField] float minPitch = -8f;
    [SerializeField] float maxPitch = 35f;
    [SerializeField] float lookAtHeight = 1.25f;
    [SerializeField] bool invertLookY = true;

    //Private variable to hold rotation
    private float yaw = 0;
    private float pitch = 0;
    private float targetYaw;
    private Camera cachedCamera;
    private AudioListener cachedAudioListener;
    private Vector2 externalLookInput;
    private bool useExternalLookInput;

    void Start()
    {
        cachedCamera = GetComponentInChildren<Camera>(true);
        cachedAudioListener = GetComponentInChildren<AudioListener>(true);

        ApplyCameraOwnershipGate();

        EnsurePreferredFollowTarget();

        if (followTarget == null)
            followTarget = ResolveDefaultFollowTarget();

        if (followTarget == null)
            return;

        targetYaw = followTarget.eulerAngles.y;
        yaw = 0f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    void OnEnable()
    {
        if (lookAction?.action != null)
            lookAction.action.Enable();
    }

    void OnDisable()
    {
        if (lookAction?.action != null)
            lookAction.action.Disable();
    }

    void LateUpdate()
    {
        ApplyCameraOwnershipGate();

        if (enableOnlyOnPreferredTank && cachedCamera != null && !cachedCamera.enabled)
            return;

        EnsurePreferredFollowTarget();

        if (followTarget == null)
        {
            followTarget = ResolveDefaultFollowTarget();
            if (followTarget == null)
                return;
        }

        //Gets look action
        Vector2 look = ReadLookInput();


        //Horizontal rotation 
        if (aimController != null)
        {
            Transform turret = aimController.GetTurretTransform();
            if (turret != null)
                targetYaw = turret.eulerAngles.y;
            else
                targetYaw = followTarget.eulerAngles.y;
        }
        else
        {
            targetYaw = followTarget.eulerAngles.y;
        }

        yaw += look.x * rotationSpeed * Time.deltaTime;
        float pitchDelta = look.y * rotationSpeed * Time.deltaTime;
        float pitchInput = invertLookY ? -pitchDelta : pitchDelta;

        pitch += pitchInput;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        pitch = Mathf.Clamp(pitch, -88.5f, 88.5f);

        //tank yaw + relative camera offset
        Quaternion yawRotation   = Quaternion.Euler(0, targetYaw + yaw, 0);
        Quaternion pitchRotation = Quaternion.Euler(pitch, 0, 0);
        Quaternion finalRotation = yawRotation * pitchRotation;

        //rotate offset around target
        transform.position = followTarget.position + finalRotation * offset;

        //transform.LookAt(followTarget.position + followTarget.up * 1.5f, followTarget.up);   
        Vector3 lookAtPoint = followTarget.position + followTarget.up * lookAtHeight;
        Vector3 lookDirection = lookAtPoint - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, followTarget.up);
        }
    }

    public void SetLookInput(Vector2 lookInput)
    {
        externalLookInput = lookInput;
        useExternalLookInput = lookInput.sqrMagnitude > 0.0001f;
    }

    public void ClearLookInput()
    {
        externalLookInput = Vector2.zero;
        useExternalLookInput = false;
    }

    Vector2 ReadLookInput()
    {
        if (useExternalLookInput)
            return externalLookInput;

        return lookAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
    }

    void ApplyCameraOwnershipGate()
    {
        if (!enableOnlyOnPreferredTank)
            return;

        bool isPreferredTank = IsPreferredTankRoot();

        if (cachedCamera != null)
            cachedCamera.enabled = isPreferredTank;

        if (cachedAudioListener != null)
            cachedAudioListener.enabled = isPreferredTank;
    }

    bool IsPreferredTankRoot()
    {
        Transform root = transform.root;
        if (root == null)
            return false;

        if (!string.IsNullOrWhiteSpace(preferredTargetName) && root.name == preferredTargetName)
            return true;

        try
        {
            if (root.CompareTag("Player") || root.CompareTag("player"))
                return true;
        }
        catch (UnityException)
        {
        }

        return false;
    }

    void EnsurePreferredFollowTarget()
    {
        if (!forcePreferredTarget)
            return;

        if (string.IsNullOrWhiteSpace(preferredTargetName))
            return;

        GameObject preferred = GameObject.Find(preferredTargetName);
        if (preferred == null)
            return;

        if (followTarget != preferred.transform)
            followTarget = preferred.transform;
    }

    static Transform ResolveDefaultFollowTarget()
    {
        GameObject preferredGreenTank = GameObject.Find("minitank-v10-green 1");
        if (preferredGreenTank != null)
            return preferredGreenTank.transform;

        GameObject greenTank = GameObject.Find("minitank-v10-green");
        if (greenTank != null)
            return greenTank.transform;

        GameObject taggedPlayer = null;
        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
        }

        if (taggedPlayer != null)
            return taggedPlayer.transform;

        TankController tankController = FindFirstObjectByType<TankController>();
        if (tankController != null)
            return tankController.transform;

        return null;
    }
}